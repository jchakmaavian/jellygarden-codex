"""Render this editable HTML offline with a dedicated headless Chrome process. Never opens Unity."""
from pathlib import Path
import argparse, base64, hashlib, json, os, socket, struct, subprocess, time, urllib.request

ROOT = Path(__file__).resolve().parent / 'Preview'
CHROME = Path(r'C:\Program Files\Google\Chrome\Application\chrome.exe')
DECODER = Path(r'C:\Program Files\Softdeluxe\Free Download Manager\ffmpeg.exe')
ENCODER = Path(r'C:\Program Files\TimeDoctor2\3.18.89\ffmpeg.exe')

class WebSocket:
    def __init__(self, url):
        from urllib.parse import urlsplit
        parsed = urlsplit(url)
        self.socket = socket.create_connection((parsed.hostname, parsed.port), timeout=20)
        key = base64.b64encode(os.urandom(16)).decode()
        request = f'GET {parsed.path} HTTP/1.1\r\nHost: {parsed.netloc}\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Key: {key}\r\nSec-WebSocket-Version: 13\r\n\r\n'
        self.socket.sendall(request.encode())
        header = b''
        while not header.endswith(b'\r\n\r\n'):
            header += self.socket.recv(1)
        assert b' 101 ' in header, header
        self.sequence = 0

    def read(self, length):
        value = b''
        while len(value) < length:
            chunk = self.socket.recv(length-len(value))
            if not chunk: raise EOFError('CDP socket closed')
            value += chunk
        return value

    def send(self, value):
        data = json.dumps(value).encode()
        mask = os.urandom(4)
        length = len(data)
        header = b'\x81' + (bytes([128 | length]) if length < 126 else b'\xfe' + struct.pack('!H',length) if length < 65536 else b'\xff'+struct.pack('!Q',length))
        self.socket.sendall(header+mask+bytes(b ^ mask[i%4] for i,b in enumerate(data)))

    def receive(self):
        full = b''
        while True:
            a,b=self.read(2)
            length=b&127
            if length==126: length=struct.unpack('!H',self.read(2))[0]
            if length==127: length=struct.unpack('!Q',self.read(8))[0]
            mask=self.read(4) if b&128 else None
            data=self.read(length)
            if mask:data=bytes(x^mask[i%4] for i,x in enumerate(data))
            if a&15==8: raise EOFError('CDP websocket closed')
            if a&15 in (0,1):full+=data
            if a&128 and full:return json.loads(full)

    def call(self, method, params=None):
        self.sequence+=1
        self.send({'id':self.sequence,'method':method,'params':params or {}})
        while True:
            response=self.receive()
            if response.get('id')==self.sequence:
                if 'error' in response:raise RuntimeError(response['error'])
                return response.get('result',{})

    def evaluate(self, expression):
        response=self.call('Runtime.evaluate',{'expression':expression,'awaitPromise':True,'returnByValue':True})
        if 'exceptionDetails' in response:raise RuntimeError(response['exceptionDetails'])
        return response.get('result',{}).get('value')

def encode(frames, output, fps):
    decode=[str(DECODER),'-v','error','-framerate',str(fps),'-i',str(frames/'frame-%04d.png'),'-f','rawvideo','-pix_fmt','rgb24','-']
    encode=[str(ENCODER),'-y','-v','error','-f','rawvideo','-pix_fmt','rgb24','-video_size','720x1280','-framerate',str(fps),'-i','-','-an','-c:v','libx264','-preset','fast','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(output)]
    decoder=subprocess.Popen(decode,stdout=subprocess.PIPE)
    encoder=subprocess.Popen(encode,stdin=decoder.stdout)
    decoder.stdout.close()
    code=encoder.wait(); decode_code=decoder.wait()
    if code or decode_code: raise RuntimeError(f'FFmpeg failed {decode_code}/{code}')

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--movie',action='store_true');parser.add_argument('--time',type=float,default=2.0)
    args=parser.parse_args()
    probe=socket.socket();probe.bind(('127.0.0.1',0));port=probe.getsockname()[1];probe.close()
    profile=ROOT/'.render-chrome-profile'
    startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
    log=(ROOT/'render-chrome.log').open('w')
    process=subprocess.Popen([str(CHROME),'--headless=new','--disable-gpu','--disable-software-rasterizer','--disable-features=VizDisplayCompositor','--no-first-run','--no-default-browser-check',f'--user-data-dir={profile}',f'--remote-debugging-port={port}','about:blank'],stdout=log,stderr=log,startupinfo=startup)
    try:
        until=time.monotonic()+15
        while True:
            try:tabs=json.load(urllib.request.urlopen(f'http://127.0.0.1:{port}/json',timeout=1));break
            except Exception:
                if time.monotonic()>until:raise
                time.sleep(.1)
        page=next(tab for tab in tabs if tab.get('type') == 'page' and tab.get('url') == 'about:blank')
        browser=WebSocket(page['webSocketDebuggerUrl'])
        browser.call('Page.enable');browser.call('Runtime.enable')
        browser.call('Emulation.setDeviceMetricsOverride',{'width':720,'height':1280,'deviceScaleFactor':1,'mobile':False})
        browser.call('Page.navigate',{'url':(ROOT/'preview.html').as_uri()+'?capture&t=0'})
        until=time.monotonic()+10
        while not browser.evaluate('typeof window.previewReady !== "undefined"'):
            if time.monotonic()>until:
                raise TimeoutError('Preview did not initialize: '+str(browser.evaluate('({url:location.href,title:document.title,body:document.body.innerText.slice(0,500),scripts:[...document.scripts].map(s=>s.src),config:typeof ROYAL_WIN})')))
            time.sleep(.05)
        browser.evaluate('window.previewReady')
        state=browser.evaluate(f'preview.seek({args.time})')
        png=browser.call('Page.captureScreenshot',{'format':'png','fromSurface':True})
        (ROOT/'preview-settled.png').write_bytes(base64.b64decode(png['data']))
        checks={'render':'offline browser preview, not a Unity capture','state':state,'fontsReady':browser.evaluate('document.fonts.check("54px RoyalDisplay")'),'imagesLoaded':browser.evaluate('[...document.images].every(i=>i.complete && i.naturalWidth>0)'),'animationSamples':[]}
        for sample in [0,.06,.15,.20,.24,.34,.36,.60,.64,.98,2,4.49,4.5,5.0,5.5,6,6.5,7,8,9.9]:checks['animationSamples'].append(browser.evaluate(f'preview.seek({sample})'))
        browser.evaluate('preview.setReduced(true);preview.seek(1.5)')
        png=browser.call('Page.captureScreenshot',{'format':'png','fromSurface':True});(ROOT/'preview-reduced-motion.png').write_bytes(base64.b64decode(png['data']))
        checks['reducedState']=browser.evaluate('preview.state()')
        browser.evaluate('preview.setReduced(false)')
        if args.movie:
            frames=ROOT/'render-frames';frames.mkdir(exist_ok=True)
            checks['frameCount']=300;checks['fps']=30
            for index in range(300):
                browser.evaluate(f'preview.seek({index/30:.8f})')
                png=browser.call('Page.captureScreenshot',{'format':'png','fromSurface':True})
                (frames/f'frame-{index:04d}.png').write_bytes(base64.b64decode(png['data']))
            encode(frames,ROOT/'Royal-Throne-Level-Complete-v5-Offline-Preview.mp4',30)
        (ROOT/'offline-checks.json').write_text(json.dumps(checks,indent=2)+'\n',encoding='utf-8')
        print(json.dumps({'screenshot':str(ROOT/'preview-settled.png'),'movie':args.movie,'fontsReady':checks['fontsReady'],'imagesLoaded':checks['imagesLoaded']}))
        browser.call('Browser.close')
    finally:
        try:process.wait(timeout=5)
        except subprocess.TimeoutExpired:process.terminate();process.wait(timeout=5)
        log.close()

if __name__=='__main__': main()
