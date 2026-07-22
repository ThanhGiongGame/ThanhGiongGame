# Game 3D Thánh Gióng (Thanh Giong Game)

[![Unity Version](https://img.shields.io/badge/Unity-6.3%20LTS-blue.svg?style=flat-square&logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP-informational.svg?style=flat-square&logo=unity)](https://unity.com/srp/Universal-Render-Pipeline)
[![License](https://img.shields.io/badge/License-MIT-green.svg?style=flat-square)](LICENSE)

> **Game 3D Thánh Gióng** là dự án game nhập vai hành động 3D được phát triển trên nền tảng **Unity 6.3 LTS** và **Universal Render Pipeline (URP)**, lấy cảm hứng từ truyền thuyết dân gian Việt Nam — Thánh Gióng cưỡi ngựa sắt, cầm tre ngà đánh đuổi giặc Ân bảo vệ quê hương.

---

## Hình Ảnh Demo (Screenshots & Gameplay)

> *Chèn hình ảnh demo của game bằng cách thêm các file ảnh vào thư mục `docs/images/`:*

| Map 1: Làng Quê Việt Nam | Map 3: Rừng Tre Hoang Sơ |
| :---: | :---: |
| ![Map 1 Làng Quê](docs/images/map1.png) | ![Map 3 Rừng Tre](docs/images/map3.png) |

| Giao Diện Chiến Đấu (Gameplay UI) | Đợt Quái Tấn Công (Wave Spawner) |
| :---: | :---: |
| ![Gameplay UI](docs/images/gameplay.png) | ![Wave Battle](docs/images/wave_battle.png) |

---

## Hướng Dẫn Chơi (How to Play & Controls)

### Mục tiêu trò chơi
- **Chiến đấu & Sống sót**: Tiêu diệt các làn sóng kẻ địch (giặc Ân, lính ném lao, lính cưỡi ngựa, quái gà...) qua từng đợt (Wave).
- **Khám phá bản đồ**: Di chuyển qua các khu vực bản đồ sinh tự nhiên (Làng quê, Rừng tre) để tiêu diệt toàn bộ giặc bảo vệ dân làng.

### Phím điều khiển (Controls)

| Thao tác | Phím thực hiện |
| :--- | :--- |
| **Di chuyển** | <kbd>W</kbd> <kbd>A</kbd> <kbd>S</kbd> <kbd>D</kbd> hoặc Phím mũi tên (<kbd>↑</kbd> <kbd>←</kbd> <kbd>↓</kbd> <kbd>→</kbd>) |
| **Tấn công / Chém** | <kbd>Chuột trái</kbd> (Left Click) hoặc Phím <kbd>Space</kbd> |
| **Lướt / Chạy nhanh (Dash)** | Phím <kbd>Left Shift</kbd> |
| **Tương tác / Kỹ năng** | Phím <kbd>E</kbd> |
| **Tạm dừng Game (Pause)** | Phím <kbd>ESC</kbd> |

---

## Tính Năng Nổi Bật (Key Features)

### 1. Hệ Thống Bản Đồ Động (Procedural Generation & Map Management)
- **Tạo Chunk ngẫu nhiên (`MapManager`)**: Sinh bản đồ liên tục và vô tận dựa trên thuật toán Chunk-based procedural generation.
- **Thuật toán tránh trùng lặp vật thể (Euclidean Distance Validation)**:
  - Tự động kiểm tra bán kính an toàn **6.0m** đối với các vật thể lớn (Nhà lá, Cây sồi, Cây liễu) để tránh hiện tượng chồng lấn vật thể.
  - Phân bổ tự nhiên cỏ dại, hoa cỏ và đá phủ nền với khoảng cách tối thiểu **1.0m**.
- **Đa dạng bản đồ (Map Themes)**:
  - **Map 1 (Làng Quê)**: Phong cảnh làng quê Việt Nam thanh bình với nhà lá, cây xanh và đồng cỏ.
  - **Map 3 (Rừng Tre)**: Cấu hình bụi tre mọc cụm (`SpawnBambooGroup`), thảm cỏ xanh dày và bóng tre hoang sơ.

### 2. Cơ Chế Sinh Quái Vật Theo Làn Sóng (Wave Spawner System)
- Quản lý các đợt tấn công của kẻ địch (`WaveSpawner`) tăng dần độ khó.
- **Tự động lọc và chuyển đổi quái vật theo đặc thù từng Map**:
  - *Map 3 (Rừng Tre)*: Tự động chuyển đổi lính cưỡi ngựa (EnemyA) thành **Quái Gà (Chicken)** và lính ném lao (EnemyC) thành **Lính Đi Bộ (EnemyB)** để phù hợp bối cảnh.

### 3. Tối Ưu Đồ Họa URP & Runtime Shader Converter
- Tự động chuyển đổi tất cả mô hình GLB/glTF dùng shader cũ sang **`Universal Render Pipeline/Lit`** ngay tại runtime.
- Xử lý triệt để các lỗi hiển thị màn hình tím/hồng (Shader Error) và tràn camera.

### 4. Hệ Thống Âm Thanh (Sound Effects & Music)
- Tích hợp hiệu ứng âm thanh chiến đấu, tiếng bước chân, tiếng chém tre và nhạc nền (BGM) chân thực.

---

## Công Nghệ Sử Dụng (Tech Stack)

- **Game Engine**: Unity 6.3 LTS
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Language**: C# (.NET Core / Mono)
- **GUI & Typography**: TextMesh Pro
- **Version Control**: Git / Git LFS

---

## Cấu Trúc Dự Án (Project Structure)

```text
ThanhGiongGame/
├── Assets/                 # Tài nguyên chính (Scripts, Prefabs, Models, Audio, Materials)
│   ├── Scripts/            # Mã nguồn C# (MapManager, WaveSpawner, PlayerController...)
│   ├── Prefabs/            # Các Prefab vật thể, quái vật, nhân vật
│   └── TextMesh Pro/       # Phông chữ & UI assets
├── docs/
│   └── images/             # Thư mục chứa hình ảnh screenshot dự án
├── ProjectSettings/        # Thiết lập cấu hình Unity (Input, Graphics, Quality, Tag...)
├── Packages/               # Các gói phụ thuộc Unity PackageManager (URP, TMP...)
├── BaoCaoThayDoi.txt       # Nhật ký chi tiết cập nhật và tối ưu hóa hệ thống
├── .gitignore              # Cấu hình bỏ qua file tạm Unity cho Git
└── README.md               # Tài liệu hướng dẫn dự án
```

---

## Hướng Dẫn Cài Đặt & Chạy Dự Án

### Yêu cầu tiên quyết
1. Đã cài đặt **Unity Hub**.
2. Đã cài đặt phiên bản **Unity 6.3 LTS** (hỗ trợ URP).

### Các bước mở dự án
1. Clone dự án về máy:
   ```bash
   git clone https://github.com/UGing265/ThanhGiongGame.git
   ```
2. Mở **Unity Hub** ➔ Chọn **Add** ➔ Trỏ tới thư mục `ThanhGiongGame`.
3. Mở dự án với phiên bản **Unity 6.3 LTS**.
4. Chờ Unity nạp Packages và Reimport tài nguyên.
5. Vào thư mục `Assets/Scenes` ➔ Mở Scene chính ➔ Bấm nút **Play** (`Ctrl + P`) để trải nghiệm game!

---

## Thành Viên Phát Triển (Team Members)

- **Giảng viên hướng dẫn**: [Tên Giảng Viên]
- **Đội ngũ sinh viên thực hiện**:
  - **UGing265** (Team Lead / Main Developer) - [GitHub Profile](https://github.com/UGing265)
  - **[Tên Thành Viên 2]** - [Vai trò: 3D Art / Map Design]
  - **[Tên Thành Viên 3]** - [Vai trò: Gameplay / Sound Design]

---

## Bản Quyền & Giấy Phép (Copyright & License)

© 2026 **UGing265** & Đội ngũ phát triển **Game 3D Thánh Gióng**. All rights reserved.

Dự án được phát hành theo giấy phép [MIT License](LICENSE). Bạn được phép tham khảo và đóng góp cho mã nguồn dự án.
