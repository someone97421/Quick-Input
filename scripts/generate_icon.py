from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets"
PNG_PATH = ASSETS / "quick-input.png"
ICO_PATH = ASSETS / "quick-input.ico"


def rounded_rectangle_mask(size: int, radius: int) -> Image.Image:
    mask = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(mask)
    draw.rounded_rectangle((0, 0, size - 1, size - 1), radius=radius, fill=255)
    return mask


def make_gradient(size: int) -> Image.Image:
    image = Image.new("RGBA", (size, size))
    pixels = image.load()
    top_left = (24, 99, 235)
    top_right = (20, 184, 166)
    bottom_left = (37, 46, 132)
    bottom_right = (14, 165, 233)

    for y in range(size):
        fy = y / (size - 1)
        for x in range(size):
            fx = x / (size - 1)
            rgb = tuple(
                int(
                    top_left[i] * (1 - fx) * (1 - fy)
                    + top_right[i] * fx * (1 - fy)
                    + bottom_left[i] * (1 - fx) * fy
                    + bottom_right[i] * fx * fy
                )
                for i in range(3)
            )
            pixels[x, y] = (*rgb, 255)

    return image


def draw_icon(size: int = 1024) -> Image.Image:
    scale = size / 1024
    image = Image.new("RGBA", (size, size), (0, 0, 0, 0))

    bg = make_gradient(size)
    bg_mask = rounded_rectangle_mask(size, int(220 * scale))
    image.alpha_composite(Image.composite(bg, Image.new("RGBA", (size, size)), bg_mask))

    draw = ImageDraw.Draw(image)

    # Soft highlight and depth for app-icon polish.
    draw.ellipse(
        [int(112 * scale), int(76 * scale), int(842 * scale), int(572 * scale)],
        fill=(255, 255, 255, 36),
    )
    draw.ellipse(
        [int(520 * scale), int(554 * scale), int(1090 * scale), int(1090 * scale)],
        fill=(0, 39, 118, 42),
    )

    shadow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    shadow_draw = ImageDraw.Draw(shadow)
    panel_box = [
        int(180 * scale),
        int(286 * scale),
        int(844 * scale),
        int(686 * scale),
    ]
    shadow_draw.rounded_rectangle(
        [
            panel_box[0],
            panel_box[1] + int(38 * scale),
            panel_box[2],
            panel_box[3] + int(38 * scale),
        ],
        radius=int(78 * scale),
        fill=(5, 16, 45, 82),
    )
    shadow = shadow.filter(ImageFilter.GaussianBlur(int(34 * scale)))
    image.alpha_composite(shadow)

    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle(
        panel_box,
        radius=int(78 * scale),
        fill=(247, 250, 252, 244),
        outline=(255, 255, 255, 180),
        width=max(1, int(8 * scale)),
    )

    header_box = [
        panel_box[0],
        panel_box[1],
        panel_box[2],
        int(398 * scale),
    ]
    draw.rounded_rectangle(
        header_box,
        radius=int(78 * scale),
        fill=(226, 238, 255, 248),
    )
    draw.rectangle(
        [header_box[0], int(350 * scale), header_box[2], header_box[3]],
        fill=(226, 238, 255, 248),
    )

    dot_y = int(340 * scale)
    for i, color in enumerate([(37, 99, 235, 255), (20, 184, 166, 255), (125, 211, 252, 255)]):
        cx = int((266 + i * 58) * scale)
        draw.ellipse(
            [cx - int(17 * scale), dot_y - int(17 * scale), cx + int(17 * scale), dot_y + int(17 * scale)],
            fill=color,
        )

    for y in [476, 558, 638]:
        draw.rounded_rectangle(
            [
                int(274 * scale),
                int(y * scale),
                int((624 if y != 558 else 554) * scale),
                int((y + 26) * scale),
            ],
            radius=int(13 * scale),
            fill=(30, 41, 59, 92),
        )

    cursor_box = [
        int(678 * scale),
        int(446 * scale),
        int(730 * scale),
        int(638 * scale),
    ]
    draw.rounded_rectangle(
        cursor_box,
        radius=int(26 * scale),
        fill=(14, 165, 233, 255),
    )
    draw.rounded_rectangle(
        [
            cursor_box[0] + int(14 * scale),
            cursor_box[1] + int(16 * scale),
            cursor_box[2] - int(14 * scale),
            cursor_box[3] - int(16 * scale),
        ],
        radius=int(12 * scale),
        fill=(255, 255, 255, 248),
    )

    # Small sync spark. It reads as "instant input" at large sizes and a bright accent in the tray.
    spark = [
        (int(754 * scale), int(244 * scale)),
        (int(816 * scale), int(244 * scale)),
        (int(782 * scale), int(344 * scale)),
        (int(862 * scale), int(344 * scale)),
        (int(704 * scale), int(570 * scale)),
        (int(746 * scale), int(414 * scale)),
        (int(678 * scale), int(414 * scale)),
    ]
    draw.polygon(spark, fill=(255, 236, 153, 255))
    draw.line(spark + [spark[0]], fill=(255, 255, 255, 150), width=max(1, int(5 * scale)), joint="curve")

    return image


def main() -> None:
    ASSETS.mkdir(parents=True, exist_ok=True)
    base = draw_icon()
    base.save(PNG_PATH)

    sizes = [16, 24, 32, 48, 64, 128, 256]
    frames = [base.resize((size, size), Image.Resampling.LANCZOS) for size in sizes]
    frames[-1].save(ICO_PATH, sizes=[frame.size for frame in frames], append_images=frames[:-1])
    print(f"Wrote {PNG_PATH}")
    print(f"Wrote {ICO_PATH}")


if __name__ == "__main__":
    main()
