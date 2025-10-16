# 🧰 Ninjadini Debug Console [NjConsole] for Unity

## ⚙️ Requirements
- **Unity 2022.3 or newer is required**    
  > NjConsole relies on Unity’s UI Toolkit at runtime, which became production-ready in Unity 2022.3 LTS.   
  > Earlier 2022 versions may have incomplete UI features or compatibility issues.   

- **Tested Platforms**:   
  Standalone Windows/Mac, iOS, Android, WebGL
  
- **Tested Editor versions**:   
  2022.3, Unity 6 - 6.3

## 🛠️ Installation
NjConsole will auto start by default.   
Refer to the demo scene below for basic usage and examples.


## 🎬 Demo
Try the included scene at Demo/Demo.unity   
If your project uses the new Input System, you may need to update the included EventSystem in the Demo scene to support it:   
In the Hierarchy, select the EventSystem object and click "Replace with InputSystemUIInputModule" in the Inspector.   
Feel free to delete the Demo folder after you're familiar with how it works.


## 📁 Documentation
Start with:   
📄 Documentation/GettingStarted.pdf

For the latest docs, advanced usage, and tips:  
🌐 https://ninjadini.github.io/njconsole/


## 👤 Credits
Created by Lu Aye Oo — lu@ninjadini.com   
UI by Jamie Simmonds   
Mono font by JetBrains


## 🔒 License
This asset is licensed under the Unity Asset Store EULA as an Extension Asset.   
https://unity.com/legal/as-terms   
Per the Extension Asset license, this asset is sold on a per-seat basis — one license is required for each individual user.   
Unauthorized sharing or resale of this asset is prohibited.

© 2025 Lu Aye Oo / Ninjadini  
All rights reserved.


## ⚠️ Known Issues
🅰️JetBrainsMono font may render certain letter pairs joined (e.g. xc, ex, ye, Sc).
This appears to affect only the Editor's Game view, not builds.


## ❓ Troubleshooting

#### I am getting errors after importing the package   
1. Make sure your Unity is at least 2022.1. (best if it is 2022.3 or newer)   
2. Are you using the new Input system? If so, ensure the Input System package is installed from Package Manager.   
   Alternatively, go to Project Settings > Player > Other Settings > Active Input Handling > choose `Input Manager (old)`

#### Key bindings doesn't work in the player build (standalone/mobile etc)   
Key bindings are disabled by default outside Editor, you can enable it from Project Settings > Ninjadini Console > Features > In Player Key Bindings

#### Overlay UI scale looks wrong in Play Mode
Unfortunately, this can happen when Unity’s Game View scaling (especially with high-DPI displays) doesn’t play nicely with UI Toolkit’s resolution scaling in certain setups.
You can manually fix it during Play Mode by opening the NjConsole overlay and adjusting the scale: `Utilities > Tools > UI Scale + / -`