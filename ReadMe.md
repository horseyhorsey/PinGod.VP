# PinGod.VP
---

A COM controller to send / receive pinball events to Godot display.

These are sent and received via OSC control on local Loopback address. Default ports 9000/9001

## Controller Registry

Run the `register-as-admin` or `unregister-as-admin` bat files to register / unregister

## Visual Pinball Setup

- Copy `core_c_sharp.vbs` and `PinGod.vbs` to `VisualPinball/Scripts`
- See `PinGodTableExample.vbs` on how to use in a visual pinball script.

## Examples

See Visual Pinball directories in games folder for examples [Examples](https://github.com/horseyhorsey/PinGod.VP.Examples)

## Adresses - OSC

- `/evt` - "game_ready"
- `/all_coils` - byte[n,2] = num, state
- `/all_lamps` - byte[n,2] = num, state
- `/all_leds` - byte[n,3] = num, state, color(ole)

When recieved these set the states in the controller, then when Visual Pinball invokes changed methods we see send back changed states.

## VP Controller Methods

### ChangedSolenoids
---

`Const UseSolenoids = 1 ' Check for solenoid states?`

### ChangedLamps
---

`Const UseLamps = True  ' Check for lamp states?`

### ChangedPDLeds
---

`Const UsePdbLeds = True  ' Check for led states?`

See `core_c_sharp.vbs`

### Display Properties
---

```
	public bool DisplayFullScreen { get; set; }
	public int DisplayWidth { get; set; }
	public int DisplayHeight { get; set; }
	public int DisplayX { get; set; }
	public int DisplayY { get; set; }
	public bool DisplayAlwaysOnTop { get; set; }
	public bool DisplayLowDpi { get; set; }
	public bool DisplayNoWindow { get; set; }
	public bool DisplayNoBorder { get; set; }
	public bool DisplayMaximized { get; set; }
```	

Visual Pinball Example:

```
	With Controller
	.DisplayX			= 10
	.DisplayY			= 10
	.DisplayWidth 		= 512 ' 1024 Original W
	.DisplayHeight 		= 300 ' 600  Original H
	.DisplayAlwaysOnTop = True
	.DisplayFullScreen 	= False 'Providing the position is on another display it should fullscreen to window
	.DisplayLowDpi 		= False
	.DisplayNoWindow 	= False
```

### Pause
---

VP `Controller.Pause 1`

Runs `SetAction` to send `pause` to Godot

### SetAction
---

VP: `Controller.SetAction "my_custom_action", 1`

Create an action in the `InputMap` settings inside godot and invoke this.

### Stop
---

VP `Controller.Stop`

### Switch
---

`Controller.Switch 69, 1`

`Controller.Switch 69, 0`

`vpmPulseSw 69`

### Run

`RunDebug GetPlayerHWnd, GameDirectory` Runs `godot` with the given project directory

`Run GetPlayerHWnd, GameDirectory` Runs an exported game executable without debug.
