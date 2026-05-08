# UnityExplorer MCP

[![LLM Tooling](https://img.shields.io/badge/LLM-Tooling-blue)](#)
[![Powered by unity-explorer](https://img.shields.io/badge/powered%20by-UnityExplorer-orange)](https://github.com/sinai-dev/UnityExplorer)

A [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) server that exposes Unity's runtime UI hierarchy to LLM agents via **UnityExplorer**. Enables agents to inspect the game scene's visual tree, search for UI elements by name/component, and retrieve real-time `RectTransform` coordinates during active gameplay.

This MCP was built and validated against **Chill with You Lo-Fi Story** (Unity 2022.3) with the **iGPU Savior** mod, but should work with any BepInEx-modded Unity game that has UnityExplorer installed.

## Quick Start

### Prerequisites

- A Unity game running with [UnityExplorer](https://github.com/sinai-dev/UnityExplorer) (for BepInEx 5)
- The game's process must be running and the target scene/scene loaded

### Install UnityExplorer (if not already)

Download `UnityExplorer.BepInEx5.Mono.zip` from the [releases page](https://github.com/sinai-dev/UnityExplorer/releases) and extract to:

```
<GameDir>\BepInEx\plugins\UnityExplorer\
```

Launch the game, then press **F7** to open UnityExplorer.

### MCP Server Configuration

Add to your MCP client config (e.g., `.claude/settings.json` or `opencode.jsonc`):

```json
"unity-explorer": {
  "command": "your-mcp-server-binary",
  "args": ["path/to/unity-explorer-mcp"],
  "env": {
    "GAME_PID": "<game-process-id>"
  }
}
```

## Available Tools

### `search_elements(query, componentType?)`

Search UI elements by name with optional component type filter.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `query` | `string` | — | Element name to search (case-insensitive partial match) |
| `componentType` | `string` | `null` | Optional component filter (`"Button"`, `"Image"`, `"Text"`, etc.). `"Text"` automatically matches `TextMeshProUGUI`. |

**Returns:** Array of matching elements with name, path, active state, and `RectTransform` data.

### `inspect_element(path)`

Get detailed component information for a specific UI element by its hierarchy path.

| Parameter | Type | Description |
|-----------|------|-------------|
| `path` | `string` | Full hierarchy path (e.g., `"Parent/Canvas/UI_FacilityNote/PotatoNoteExportButton"`) or element name |

**Returns:** The element's transform data, all components with their key property values, and child element list.

### `get_ui_hierarchy(canvasName?)`

Get the complete UI hierarchy tree with `RectTransform` coordinates for all active canvases.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `canvasName` | `string` | all | Optional canvas name filter (case-insensitive) |

**Returns:** Full tree of Canvas elements with positions, sizes, anchors, and child nesting.

## Common Workflows

### Locating a button by name

```
search_elements(query: "MyButton")
→ [{name: "MyButton", path: "Canvas/HUD/MyButton", rectTransform: {posX: ..., posY: ...}}]
```

### Inspecting a UI element's position

```
inspect_element(path: "Canvas/HUD/MyButton")
→ {rectTransform: {anchoredPosition, sizeDelta, anchorMin, anchorMax, pivot, ...}}
```

### Understanding overall UI layout

```
get_ui_hierarchy(canvasName: "MainCanvas")
→ Full tree with all children and their coordinates
```

## Coordinate Fields Explained

Each element's `rectTransform` contains:

| Field | Description |
|-------|-------------|
| `posX`, `posY` | World/screen-space position |
| `width`, `height` | Rendered dimensions |
| `anchorMinX/Y` | Anchor minimum (0-1 normalized) |
| `anchorMaxX/Y` | Anchor maximum (0-1 normalized) |
| `pivotX/Y` | Pivot point (0-1 normalized) |
| `localPosX/Y` | Position relative to parent |
| `scaleX/Y` | Local scale |
| `rotation` | Local rotation in degrees |

## Tips for LLM Agents

- Use `search_elements` first to discover element paths, then `inspect_element` with the full path for detailed data
- `posY` decreases going up (Unity's coordinate system); compare `posY` values to check vertical alignment
- For dynamic elements (instantiated at runtime prefabs), the path starts from the scene root object that owns the Canvas
- `componentType: "Text"` works for both `Text` (uGUI legacy) and `TextMeshProUGUI` components

## Origin

This MCP was created during development of the [iGPU Savior](https://github.com/anomalyco/iGPUSaviorMod) mod to assist with precise UI alignment and debugging. It was originally designed for the LLM to inspect runtime Unity UI element positions without needing screenshots or manual `RectTransform` reading.

## License

See [LICENSE](../../../LICENSE.txt).
