"""QA contact sheet from actual Game-view screenshots; no asset painting."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

task = Path(__file__).resolve().parents[1]
board = Image.new('RGB', (1080, 430), (20, 46, 82))
draw = ImageDraw.Draw(board)
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 22)
for index, (name, label) in enumerate((('Mouse', 'Mouse right'), ('Hint', 'Match hint below'), ('Touch', 'Touch left'))):
    image = Image.open(task / 'After' / f'Attention-{name}.png').convert('RGB')
    assert image.size == (1080, 1920), image.size
    # Preserve the pixels and aspect ratio of the actual central HUD frame.
    crop = image.crop((360, 35, 720, 415))
    board.paste(crop, (index * 360, 45))
    draw.text((index * 360 + 15, 12), label, font=font, fill='white')
board.save(task / 'After' / 'Attention-Comparison.png')
