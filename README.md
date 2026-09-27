# Unity Firebase Service (UPM Package)

Package module quản lý và kết nối toàn diện hệ sinh thái **Google Firebase** (Firebase Analytics, Remote Config, Ad Revenue Attribution & Editor Sync Tooling) cho **Unity Core Framework**.

Được tích hợp sẵn với kiến trúc **Pluggable Service Bridge** (`Unity.Core`), cho phép gọi các dịch vụ phân tích, cấu hình từ xa và theo dõi doanh thu quảng cáo an toàn, không lo crash ngay cả khi chạy trên Editor hoặc mất kết nối mạng.

---

## 🚀 Các Tính Năng Nổi Bật

1. **Firebase Analytics**:
   - Tự động gắn kết với `Unity.Core.Services.Analytics.AnalyticsService`.
   - Cung cấp Fluent API `LogEventParameter` và bộ hàm chuẩn hóa theo dõi Ad Formats (App Open, Banner, Interstitial, Rewarded, MRec) và Gameplay.
   - Thiết lập User Property (phương thức đăng nhập tự động Google/Apple, User ID).

2. **Firebase Remote Config & Local Fallback**:
   - Tự động gắn kết với `Unity.Core.Services.RemoteConfig.RemoteConfigService`.
   - Đọc cấu hình an toàn generic: `RemoteConfigService.GetValue<T>(key, defaultValue)`.
   - Tự động nạp default JSON từ thư mục `Resources/` (`RemoteConfig_Android.json`, `RemoteConfig_iOS.json`, `RemoteConfig.json`).
   - Tự động cache mã hóa cấu hình mới nhất vào `Application.persistentDataPath/config_local`.

3. **Impression-Level Ad Revenue Tracking**:
   - Tự động gắn kết với `Unity.Core.Services.Tracking.TrackingService`.
   - Bắt sự kiện doanh thu `AdRevenueInfo` từ AppLovin MAX / AdMob và tự động bắn event chuẩn `ad_impression` lên Firebase Analytics.

4. **Unity Editor Sync Tool (OAuth2 Google REST API)**:
   - Menu: **`Unity Core > Firebase > Remote Config Sync Tool`**.
   - Tự động xác thực qua file Service Account Key `.json` và tải Template Remote Config trực tiếp từ Google Cloud về file `Resources/RemoteConfig.json`.

5. **IL2CPP Stripping Safe**:
   - Kèm file `link.xml` bảo vệ các symbol của Firebase SDK, Google APIs và JSON serializer khỏi bị strip khi build release với Managed Stripping Level = High.

---

## 📦 Cài Đặt Vào Dự Án

### Cách 1: Cài đặt qua Git URL trong Unity Package Manager
1. Mở Unity Editor: **Window** > **Package Manager**.
2. Nhấn vào dấu **`+`** (góc trên bên trái) > chọn **Add package from git URL...**
3. Nhập:
   ```text
   https://github.com/thoxuong92/com.unity.firebase.git
   ```

### Cách 2: Qua file `Packages/manifest.json`
Thêm dependency trỏ tới kho lưu trữ GitHub:
```json
{
  "dependencies": {
    "com.unity.core": "https://github.com/thoxuong92/com.unity.core.git",
    "com.unity.firebase": "https://github.com/thoxuong92/com.unity.firebase.git"
  }
}
```

---

## 🛠️ Yêu Cầu Phụ Thuộc (Dependencies)

- **Unity**: 2021.3 trở lên
- **com.unity.core**: `1.0.0`
- **Firebase SDK for Unity**: (Analytics, Remote Config) tuỳ chọn khi build device.

---

## 👨‍💻 Tác Giả & Bản Quyền
- **Tác giả**: **joukyuu**
- **Repository**: [thoxuong92/com.unity.firebase](https://github.com/thoxuong92/com.unity.firebase.git)
