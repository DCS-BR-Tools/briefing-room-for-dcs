# BriefingRoom for DCS World

## Quick Start

This folder contains three applications for generating DCS World missions:

### BriefingRoom-Desktop.exe (Recommended)
**Full-featured graphical interface**

```
BriefingRoom-Desktop.exe
```

Double-click to launch. Requires [WebView2 Runtime](https://go.microsoft.com/fwlink/p/?linkid=2124703) (usually pre-installed on Windows 10/11).

> **Note:** Do not place in `Program Files` or `Program Files (x86)` folders.

### BriefingRoom-Web.exe
**Web server with browser-based interface**

```
BriefingRoom-Web.exe
```

Starts a local web server. Open http://localhost:5000 in your browser.
Useful for running on a server or accessing from other devices on your network.

### BriefingRoom-CLI.exe
**Batch mission generation for automation**

```
BriefingRoom-CLI.exe template.brt
BriefingRoom-CLI.exe template1.brt template2.brt template3.brt
```

Generate missions from template files without a GUI. Perfect for scripting and automation.

---

## Folder Structure

```
├── BriefingRoom-Desktop.exe  # Desktop GUI application
├── BriefingRoom-Web.exe      # Web server application
├── BriefingRoom-CLI.exe      # Command-line tool
├── README.md                 # This file
│
├── bin/                      # Application data (DO NOT DELETE)
│   ├── Database/             # Unit definitions, coalitions, theaters
│   ├── DatabaseJSON/         # JSON data files
│   ├── CustomConfigs/        # Your custom configurations
│   ├── Include/              # Lua scripts, HTML templates
│   └── Media/                # Images and icons
│
└── wwwroot/                  # Web assets (for BriefingRoom-Web.exe)
```

### Modding & Customization

- **Custom units/coalitions**: Edit files in `bin/Database/` and `bin/DatabaseJSON/`
- **Custom configs**: Add your configurations to `bin/CustomConfigs/`
- **Lua scripts**: Modify mission scripts in `bin/Include/Lua/`

See the full documentation at: https://github.com/DCS-BR-Tools/briefing-room-for-dcs

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| BriefingRoom-Desktop.exe won't start | Install [WebView2 Runtime](https://go.microsoft.com/fwlink/p/?linkid=2124703) |
| "Access denied" errors | Move folder outside of `Program Files` |
| Missing database errors | Ensure `bin/` folder is present with all subfolders |
| BriefingRoom-Web.exe port conflict | Check if port 5000 is available or set `ASPNETCORE_URLS` environment variable |

## License

GNU General Public License v3.0 - Free and open source forever.
