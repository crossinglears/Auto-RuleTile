# Core
<img src="https://github.com/user-attachments/assets/635f1ca3-ccb6-4b10-b55e-514b805c2d91" align="right" width="250">

The core component of Crossing Lears tools

This package contains tools and extensions that will mostly be used by different tools created by [Crossing Lears](https://crossinglears.carrd.co/)

### Why you should use the Crossing Lears tools?
Crossing Lears creates tools that aims to make game development in Unity Engine more convenient.

## Installation
### Unity 2019.2 or Later (via Unity Package Manager)
1. Open **Window** → **Package Manager**
2. Click **Add package from git URL**
3. Paste the following URL:
   ```
   https://github.com/crossinglears/Core.git#main
   ```

---

## Features & Contents
### Attributes
- `[Button]` – Exposes a method as a button in the **Inspector**
  - ` [Button] `
  - ` [Button("Custom Label")] `
- `[ReadOnly]` – Makes a field **readonly** in the **Inspector**

### Context Menus
- `Autograb` – Fills all empty public/SerializedFields in the selected component  
  _Right-click on a component → Select **Autograb**_  
  _Undo changes with **CTRL+Z**_
- `MoveToTop` – Moves a MonoBehaviour to the top of the component list  
  _Right-click on a MonoBehaviour → Select **MoveToTop**_

### Extensions
- `Instantiate(Object)` – Simplifies object instantiation
- `Destroy(Object)` – Streamlines object destruction
- `Close(GameObject) / Open(GameObject)` – Easily toggle GameObject visibility

### Extensions
- ...soon
  
### Base Classes
- `Pool` – Simple gameobject pool

### Windows
- ...soon

### MonoBehaviours
- `PlatformDependent` – Restricts a GameObject to specific platforms
- `OnEnableScript` – Exposes gameobject OnEnable and OnDisable events in the editor

---

More features and improvements coming soon.

