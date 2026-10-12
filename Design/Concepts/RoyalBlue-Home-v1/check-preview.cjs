const fs=require('node:fs');
const path=require('node:path');
const {pathToFileURL}=require('node:url');
const {chromium}=require('playwright');
const concept=__dirname;
const preview=process.argv[2];
const out=path.join(concept,'review');
const assert=(value,message)=>{if(!value)throw Error(message);};
(async()=>{
  fs.mkdirSync(out,{recursive:true});
  const browser=await chromium.launch({headless:true,channel:'msedge'});
  try{
    const page=await browser.newPage({viewport:{width:736,height:1150},deviceScaleFactor:1});
    const errors=[];page.on('pageerror',error=>errors.push(error.message));
    await page.goto(pathToFileURL(preview).href);
    const scope=page.frameLocator('iframe');
    await scope.locator('#rt-home-study').waitFor();
    let frame=page.frames().find(f=>f!==page.mainFrame());
    await scope.locator('img').evaluateAll(images=>Promise.all(images.map(i=>i.decode())));
    await frame.evaluate(()=>document.fonts.ready);
    assert(await scope.locator('img').evaluateAll(images=>images.every(i=>i.naturalWidth>0)),'Broken image');
    const results=[];
    for(const width of [320,360,432,736]){
      await page.setViewportSize({width,height:1150});
      for(const format of ['tall','short']){
        await frame.evaluate(format=>{document.getElementById('rt-home-study').dataset.format=format;},format);
        const layout=await frame.evaluate(()=>{
          const root=document.getElementById('rt-home-study'),phone=root.querySelector('.rt-phone').getBoundingClientRect();
          const buttons=[...root.querySelectorAll('button')].filter(b=>b.getClientRects().length).map(b=>{const r=b.getBoundingClientRect();return{label:b.getAttribute('aria-label'),x:r.x,y:r.y,width:r.width,height:r.height,right:r.right,bottom:r.bottom};});
          const overlaps=[];for(let i=0;i<buttons.length;i++)for(let j=i+1;j<buttons.length;j++){const a=buttons[i],b=buttons[j];if(Math.min(a.right,b.right)-Math.max(a.x,b.x)>1&&Math.min(a.bottom,b.bottom)-Math.max(a.y,b.y)>1)overlaps.push([a.label,b.label]);}
          return {frameWidth:innerWidth,phoneWidth:phone.width,phoneHeight:phone.height,overflow:document.documentElement.scrollWidth>innerWidth+1,buttons,overlaps,controlsOutside:buttons.filter(b=>b.x<phone.x-1||b.right>phone.right+1||b.bottom>phone.bottom+1)};
        });
        assert(!layout.overflow,`Horizontal overflow at ${width}/${format}`);
        assert(layout.buttons.every(b=>b.width>=43.5&&b.height>=43.5),`Small touch target at ${width}/${format}: `+JSON.stringify(layout.buttons.filter(b=>b.width<43.5||b.height<43.5)));
        assert(!layout.overlaps.length,`Overlapping controls at ${width}/${format}: `+JSON.stringify(layout.overlaps));
        assert(!layout.controlsOutside.length,`Control outside phone at ${width}/${format}: `+JSON.stringify(layout.controlsOutside));
        results.push({width,format,...layout});
      }
    }
    await page.setViewportSize({width:736,height:1150});
    await frame.evaluate(()=>{document.getElementById('rt-home-study').dataset.format='tall';document.getElementById('rt-home-study').dataset.motion='off';});
    await frame.evaluate(()=>document.getElementById('rt-home-study').dataset.format='short');
    await frame.evaluate(()=>document.getElementById('rt-home-study').dataset.format='tall');
    // Run interaction checks in a fresh sandbox after the viewport geometry sweep.
    await page.reload();await scope.locator('#rt-home-study').waitFor();
    frame=page.frames().find(f=>f!==page.mainFrame());
    await scope.locator('img').evaluateAll(images=>Promise.all(images.map(i=>i.decode())));
    const interactions=[];
    await frame.evaluate(()=>{globalThis.previewEvents=[];for(const name of ['pointerdown','pointerup','click'])document.addEventListener(name,e=>previewEvents.push({type:name,target:e.target.className,button:e.target.closest('button')?.getAttribute('aria-label')}),true);});
    for(const key of ['profile','coins','lives','stars','settings','level','area','story','leagues','team','collection']){
      // Let the browser update hit regions after the previous dialog closes.
      await frame.evaluate(()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve))));
      const trigger=scope.locator('[data-open='+key+']');
      const bounds=await trigger.boundingBox();
      const hit=await page.evaluate(r=>({hit:document.elementFromPoint(r.x+r.width/2,r.y+r.height/2)?.tagName,scroll:scrollY}),bounds);
      await trigger.click();
      if(!await scope.locator('.rt-dialog').isVisible()){await page.screenshot({path:path.join(out,'interaction-failure.png'),fullPage:true});throw Error('Missing dialog '+key+': '+JSON.stringify({bounds,hit,iframe:await page.locator('iframe').boundingBox(),child:await frame.evaluate(()=>({events:previewEvents.slice(-12),focused:document.activeElement.outerHTML.slice(0,250),inert:document.querySelector('.rt-nav').inert,veil:document.querySelector('.rt-veil').hidden,scroll:scrollY,phoneScroll:document.querySelector('.rt-phone').scrollTop}))}));}
      assert(await scope.locator('.rt-dialog-crest').evaluate(i=>i.complete&&i.naturalWidth>0),'Missing dialog icon '+key);
      assert(await scope.locator('.rt-close').evaluate(el=>el===document.activeElement),'Focus did not enter dialog '+key);
      const title=await scope.locator('#rt-home-dialog-title').textContent();
      if(key==='settings'){
        await scope.locator('.rt-motion-row input').uncheck();
        assert(await scope.locator('#rt-home-study').getAttribute('data-motion')==='off','Motion toggle failed');
        await scope.locator('.rt-motion-row input').check();
      }
      await scope.locator('.rt-close').press('Shift+Tab');
      assert(await scope.locator('.rt-dialog-cta').evaluate(el=>el===document.activeElement),'Backward focus trap failed');
      await scope.locator('.rt-dialog-cta').press('Tab');
      assert(await scope.locator('.rt-close').evaluate(el=>el===document.activeElement),'Forward focus trap failed');
      await scope.locator('.rt-close').press('Escape');
      assert(!await scope.locator('.rt-dialog').isVisible(),'Escape close failed '+key);
      assert(await trigger.evaluate(el=>el===document.activeElement),'Focus return failed '+key);
      interactions.push({key,title,passed:true});
    }
    await scope.locator('[data-open=area]').click();await scope.locator('.rt-dialog-cta').click();
    assert(!await scope.locator('.rt-dialog').isVisible(),'Back to home failed');
    await scope.locator('[data-home]').click();
    const before=await scope.locator('.rt-glint').evaluate(el=>getComputedStyle(el).opacity);
    await page.waitForTimeout(240);
    const after=await scope.locator('.rt-glint').evaluate(el=>getComputedStyle(el).opacity);
    assert(before!==after,'Ambient animation is static');
    await page.emulateMedia({reducedMotion:'reduce'});
    assert(await scope.locator('.rt-glint').evaluate(el=>getComputedStyle(el).animationName)==='none','Reduced motion not respected');
    assert(!errors.length,'Browser errors: '+errors.join('; '));
    // Capture print-resolution review images separately from input tests. Edge's
    // fractional-DPR screenshot capture can leave stale iframe pointer hit regions.
    const capture=await browser.newPage({viewport:{width:736,height:1150},deviceScaleFactor:2.5});
    await capture.goto(pathToFileURL(preview).href);
    const captureScope=capture.frameLocator('iframe');
    await captureScope.locator('img').evaluateAll(images=>Promise.all(images.map(i=>i.decode())));
    const captureFrame=capture.frames().find(f=>f!==capture.mainFrame());
    await captureFrame.evaluate(()=>{document.getElementById('rt-home-study').dataset.motion='off';return document.fonts.ready;});
    await captureScope.locator('.rt-phone').screenshot({path:path.join(out,'home-1080x2400.png')});
    await captureFrame.evaluate(()=>document.getElementById('rt-home-study').dataset.format='short');
    await captureScope.locator('.rt-phone').screenshot({path:path.join(out,'home-1080x1920.png')});
    await captureFrame.evaluate(()=>document.getElementById('rt-home-study').dataset.format='tall');
    await captureScope.locator('[data-open=area]').click();
    await captureScope.locator('.rt-phone').screenshot({path:path.join(out,'area-reward-preview.png')});
    await capture.close();
    const report={status:'passed',scope:'Browser design preview only. Not a Unity, device-performance, game-logic, or user-preference test.',imageDecode:true,layouts:results,interactions,focusTrap:true,returnToHome:true,ambientMotion:true,reducedMotion:true,consoleErrors:errors};
    fs.writeFileSync(path.join(concept,'preview-checks.json'),JSON.stringify(report,null,2)+'\n');
    console.log(JSON.stringify({status:report.status,layouts:results.length,interactions:interactions.length,screenshots:out,errors},null,2));
  }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
