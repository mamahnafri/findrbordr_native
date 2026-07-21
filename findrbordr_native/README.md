\# FindrBordr Native (WPF Explorer Overlay) 


\## Screenshot

!\[Preview](ss.jpg)

Aplikasi WPF C# berbasis Windows yang menempel (\*overlay\*) secara di atas jendela Windows File Explorer (`CabinetWClass`). Proyek ini dibuat karena terinspirasi oleh aplikasi mydockfinder dan borderskin.
https://github.com/mohamedkomalo/BorderSkin
https://github.com/mydockfinder/mydockfinder-for-Win10-Win11


Warning!
Proyek ini dibuat sepenuhnya menggunakan AI-assisted coding / Vibe Coding. Kode dirancang untuk kebutuhan uji coba fungsionalitas (PoC) dan kustomisasi personal. Masih banyak bug dimana-mana. Maka dari itu saya sangat berharap komunitas dapat membantu mengembangkan dan menyempurnakannya hingga 100% akurat sesuai aslinya.
 



\## Fitur Utama

\- \*\*Window Docking \& Syncing\*\*: Menempel pada File Explorer menggunakan Win32 `SetWindowPos` dan event hook `SetWinEventHook`.

\- \*\*Integrated Navigation\*\*: Kontrol toolbar bawaan (Back, Forward, Up, View, Search, dll) menggunakan `SendKeys`.

\- \*\*Parallax Wallpaper Effect\*\*: Latar belakang jendela menyesuaikan dengan wallpaper desktop Windows secara akurat.

\- \*\*Custom Shortcuts\*\*: Simpan jalan pintas folder/file lokal melalui fitur Drag \& Drop.



\## Persyaratan Sistem

\- Windows 10 / Windows 11

\- .NET 6.0 / .NET 8.0 SDK

\- Visual Studio 2022



Todo:
1. Menambahkan dark mode
2. Memperbaiki masalah focus window explorer
3. Mengimplementasikan refraction pada sidebar
4. Membuat toolbar button dan custom shortcut bisa dipindah posisi

