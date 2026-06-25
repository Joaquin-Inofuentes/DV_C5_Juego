import os
from PIL import Image, ImageDraw

def generate_sprites():
    # Target directory inside Unity Assets
    target_dir = r"c:\Users\PC_JOACO\Documents\DV_C5_Juego\Assets\_Redes\Art\Textures"
    os.makedirs(target_dir, exist_ok=True)
    
    # 1. Cursor Normal / Base Circle (simple dot with crosshair or a neat cursor shape)
    # Let's make a beautiful 32x32 retro crosshair / ring
    cursor_base = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
    draw = ImageDraw.Draw(cursor_base)
    # Circle outline
    draw.ellipse([8, 8, 23, 23], outline=(255, 255, 255, 255), width=2)
    # Dot in center
    draw.ellipse([14, 14, 17, 17], fill=(255, 255, 255, 255))
    cursor_base.save(os.path.join(target_dir, "CursorBase.png"))
    
    # 2. Shoot Cursor / Fire effect (white dot with expanding fire rings or spikes)
    cursor_shoot = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
    draw = ImageDraw.Draw(cursor_shoot)
    # Center dot
    draw.ellipse([14, 14, 17, 17], fill=(255, 255, 0, 255)) # Yellow center
    # Crosshair lines showing fire spike
    draw.line([15, 2, 15, 10], fill=(255, 200, 0, 255), width=2)
    draw.line([15, 21, 15, 29], fill=(255, 200, 0, 255), width=2)
    draw.line([2, 15, 10, 15], fill=(255, 200, 0, 255), width=2)
    draw.line([21, 15, 29, 15], fill=(255, 200, 0, 255), width=2)
    # Outer circle red-orange
    draw.ellipse([6, 6, 25, 25], outline=(255, 100, 0, 255), width=2)
    cursor_shoot.save(os.path.join(target_dir, "CursorShoot.png"))
    
    # 3. Hit Cursor (Red target / cross / red lines)
    cursor_hit = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
    draw = ImageDraw.Draw(cursor_hit)
    # Red X crosshair
    draw.line([4, 4, 12, 12], fill=(255, 0, 0, 255), width=3)
    draw.line([19, 19, 27, 27], fill=(255, 0, 0, 255), width=3)
    draw.line([27, 4, 19, 12], fill=(255, 0, 0, 255), width=3)
    draw.line([12, 19, 4, 27], fill=(255, 0, 0, 255), width=3)
    # Red circle
    draw.ellipse([9, 9, 22, 22], outline=(255, 0, 0, 255), width=2)
    cursor_hit.save(os.path.join(target_dir, "CursorHit.png"))

    # 4. Loader Circle / Reload Sprite (A simple 32x32 clean circle texture to be used as UI Radial Image)
    # The radial animation will be driven by Unity's Image component Fill Method (Radial 360)
    # So we just need a solid clean white circle ring for the radial fill!
    cursor_reload = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
    draw = ImageDraw.Draw(cursor_reload)
    draw.ellipse([2, 2, 29, 29], outline=(255, 255, 255, 255), width=4)
    cursor_reload.save(os.path.join(target_dir, "CursorReload.png"))

    print("Successfully generated all 4 cursors!")

if __name__ == '__main__':
    generate_sprites()
