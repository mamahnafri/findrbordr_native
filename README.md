# 🖼️ FindrBordr Native (WPF Explorer Overlay)

[![Platform](https://img.shields.io/badge/Platform-Windows-0078D6?style=for-the-badge&logo=windows)](https://microsoft.com)
[![Framework](https://img.shields.io/badge/.NET-6.0%20%7C%208.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/Language-C%23-239120?style=for-the-badge&logo=c-sharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Development](https://img.shields.io/badge/Mode-Vibe%20Coding%20%2F%20AI%20Assisted-FF6F61?style=for-the-badge)](https://github.com)

Aplikasi WPF C# berbasis Windows yang menempel (*overlay*) secara presisi di atas jendela Windows File Explorer (`CabinetWClass`). 

---

## 📸 Preview

![Preview](ss.jpg)

---

## 💡 Inspirasi & Kredit Proyek

Proyek ini terinspirasi dari aplikasi kustomisasi antarmuka Windows ternama:
* 🛠️ **[MyDockFinder](https://github.com/mydockfinder/mydockfinder-for-Win10-Win11)**
* 🖼️ **[BorderSkin](https://github.com/mohamedkomalo/BorderSkin)**

---

> [!WARNING]
> **Proyek Eksperimental / Proof of Concept (PoC)**  
> Proyek ini dibangun sepenuhnya menggunakan **AI-assisted / Vibe Coding**. Kode dirancang untuk kebutuhan uji coba fungsionalitas dan kustomisasi personal, sehingga masih memiliki beberapa *bug* dan keterbatasan.  
> 
> 🤝 **Kontribusi Sangat Diharapkan!** Komunitas dan pengembang lain sangat disambut hangat untuk membuka *Issue* maupun *Pull Request (PR)* demi menyempurnakan aplikasi ini agar lebih akurat dan stabil!

---

## ✨ Fitur Utama

- 📌 **Window Docking & Syncing**  
  Menempel dan mengikuti pergerakan File Explorer secara *real-time* menggunakan Win32 `SetWindowPos` dan event hook `SetWinEventHook`.
- 🕹️ **Integrated Navigation Toolbar**  
  Eksekusi kontrol bawaan Explorer (Back, Forward, Up, View, Search, dll.) secara langsung memanfaatkan Win32 `SendKeys`.
- 🌌 **Parallax Wallpaper Effect**  
  Latar belakang jendela menyesuaikan dengan posisi wallpaper desktop Windows untuk memberikan efek transparan yang dinamis.
- 📁 **Custom Shortcuts via Drag & Drop**  
  Tambahkan jalan pintas folder atau file lokal favorit secara instan hanya dengan menyeret (*drag*) file ke area yang disediakan.

---

## 💻 Persyaratan Sistem

| Komponen | Spesifikasi Minimum |
| :--- | :--- |
| **Sistem Operasi** | Windows 10 / Windows 11 |
| **Runtime** | .NET 6.0 SDK / .NET 8.0 SDK |
| **IDE (Opsional)** | Visual Studio 2022 / VS Code |

---

## 🗺️ Roadmap & To-Do

- [ ] 🌙 Menambahkan opsi **Dark Mode**
- [ ] 🎯 Memperbaiki manajemen fokus jendela File Explorer saat diklik
- [ ] 🧪 Mengimplementasikan efek *refraction* pada bagian sidebar
- [ ] 🔀 Fitur kustomisasi posisi untuk tombol toolbar dan *custom shortcut*

---

## 🤝 Kontribusi

Jalur kontribusi selalu terbuka! Jika Anda memiliki ide perbaikan arsitektur kode, perbaikan *bug*, atau peningkatan visual, silakan kirimkan *Pull Request*.
