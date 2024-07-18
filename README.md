# Gimmick ROM extractor
This application allows extracting a raw NES ROM from the game files included with the Steam Release of GIMMICK! Special Edition

## Usage
1. Put Gimmick_ROM_extractor.exe and config.json into the same directory as AR_win32.mdf, which is the resource file that contains the ROM in an encrypted format.
For a default steam installation the path should look something like this: ```C:\Program Files (x86)\Steam\steamapps\common\Gimmick! Special Edition\TRICK\```
2. Open Gimmick_ROM_extractor.exe and you should be prompted if you want to extract the ROM from AR_win32.mdf. Hit y and after a second or so the ROM should be written to ```C:\Program Files (x86)\Steam\steamapps\common\Gimmick! Special Edition\TRICK\Gimmick! (Japan).nes```

![image](https://raw.githubusercontent.com/Infinest/Gimmick-ROM-extractor/master/Images/cmd.jpg)

## Extracting ROMs from other releases
Currently the extractor supports ROM extraction from the following, other releases:
- [Abarenbo Tengu & Zombie Nation](https://store.steampowered.com/app/1603920)
- [F-117A Stealth Fighter](https://store.steampowered.com/app/1245170)

For this to work, do the exact same as with Gimmick but use the config.json from the "alternate_configs" folder that corresponds to the game.
