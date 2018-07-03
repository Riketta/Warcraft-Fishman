# Warcraft Fishman
## Description
Simple fishing bot that emulates user input via keyboard and mouse. No injection or other dangerous detectable moves.  
It works well and have pretty high accuracy. Uses presets to manage different actions.

## Recomendations
- Game should be launched in windowed fullscreen mode
- Use first person view camera
- Also absence of other selectable objects (NPCs, for example) in background would be good, but not necessary
- Use default GCD = 1500 for each action in your presets (except fishing action, it has 300 ms GCD)
- Use "Oversized Bobber" if you fishing pools, but default bobber have a bit higher accuracy in water without pools
- Bobber should stay in front of pool on its border (closer to you), because little fishes have higher priority for cursor targeting and bot can't detect bobbers positioning.
- Water horizont (absolute, like in ocean) should be around 20-25% from top
- Have SpellQueueWindow 100 or higher (check it with "/dump GetCVar("SpellQueueWindow")" and set with "/console SpellQueueWindow 200")
- Have at least 50 FPS

## Presets
### Preset configuration:  
- **Name**: feel free to use anything, but take into account that this name used as filename when bot saves config. 
		So don't use special characters such as: ", <, >, |, :, *, ?, \, /

### Action configuration:
**Event types**:
- **None**: actions with such type will be ignored. Don't use it in real presets
- **Fish**: fishing action as it is. Should be only one action of this type per preset
- **Once**: called once before first fishing iteration. Usage examples: equip rod, open bag, zoom-in camera
- **PreFish**: called once before each fishing iterations. Usage examples:
- **PostFish**: called after each successful fishing iteration. Usage examples: throw fish with macro (/cast Oodelfjisk)
- **Interval**: called once in Interval (check this field). First call works same as PreFish. Usage examples: update lures, update Oversized Bobber, etc

**Fields**:
- **Description**: just name or description of action
- **Key**: any key from https://www.pinvoke.net/default.aspx/Enums.VirtualKeys in string representation. For example: N1, N2, Q, E, F3, Numpad1
- **Trigger**: one of events described earlier
- **GCD**: global spell cooldown in milliseconds. Default 1500. Recommended to use default value. Default value for Fish event 300
- **CastTime**: spell cast time in milliseconds. 0 means instant. **Be careful**: some spells have GCD after cast, so you have to add this values into CastTime (look how Oversized Bobber works in this way)
- **Interval**: inverval in seconds between action calls. Used only with Interval event type.

### Example (Data\Main.json)
~~~~
{
  "Name": "Main",
  "Actions": [
    {
      "Description": "Fishing",
      "Key": "N1",
      "Trigger": "Fish",
      "GCD": 300,
      "CastTime": 22000,
      "Interval": 0
    },
    {
      "Description": "Oversized Bobber",
      "Key": "N2",
      "Trigger": "Interval",
      "CastTime": 3000,
      "Interval": 1800
    },
	{
      "Description": "Arcane Lure",
      "Key": "N3",
      "Trigger": "Interval",
      "Interval": 600
    },
	{
      "Description": "Ancient Vrykul Ring",
      "Key": "N4",
      "Trigger": "Interval",
      "Interval": 1800
    },
	{
      "Description": "Throw Oodelfjisk",
      "Key": "N5",
      "Trigger": "PostFish",
    }
  ]
}
~~~~

## Dump mode (--dump)
Saves icons once per second.  
Capture by yourself fishing icon (fishhook) and default icon (hand), and save it as **fishhook.bmp** and **default.bmp**.  

## Command-line interface
* -s, --save: saves default preset into file that can be used as preset reference.
* -p, --preset: path to selected preset. Example: --preset margoss.json
* -d, --dump: runs in dump mode. Use it alone.
* --help: display help screen.
* --version: display version information.

## Author
Developed by Riketta (rowneg@bk.ru / github.com/riketta).  
Based on https://github.com/trenus/Bots.WoW.Fishing by Trenus.  
The original project was rewritten from scratch and has only a few original functions.