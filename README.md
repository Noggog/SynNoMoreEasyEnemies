# NoMoreEasyEnemies
Rework NPC leveled list difficulty multipliers and settings to banish low-level enemies from your high-level dungeons.

## Wait, what?
The way Skyrim chooses which NPC to place in a dungeon is...complicated. Long story short, it uses a combination of your level, actor multipliers, the spawn's difficulty setting, and some flags to determine which level of an NPC is standing in a particular spot. 

The problem is that one of the spawn difficulties, "Easy", chooses from the list in such a way that it has the (quite good) chance to spawn the lowest-level enemies. There is a flag to turn this off, which "Easy" happily ignores. So any time you have an "Easy" spawn in a dungeon, there's a chance for you to see a low-level enemy in that spot.

This patcher reworks the multipliers and the spawn settings to avoid this issue with the "Easy" setting.  It optionally updates all Leveled NPC lists to prevent low-level enemies from being spawned.

## How?
The easiest way to explain the HOW is with an example.

By default, Skyrim uses the following spawn difficulty (leveled actors) multipliers:
- fLeveledActorMultEasy: 0.33
- fLeveledActorMultMedium: 0.67
- fLeveledActorMultHard: 1.00
- fLeveledActorMultVeryHard: 1.25

Critically, if a spawn does NOT have a difficulty set, the multiplier used is a static 1.00.  We can abuse this fact to avoid the Easy issue.

If you leave the "Level Modifier to Replace" setting at the default of Hard, NoMoreEasyEnemies will do the following:
- Change fLeveledActorMultMedium to 0.33 (new Easy)
- Change fLeveledActorMultHard to 0.67 (new Medium)
- For all enemy spawns (Placed NPCs), if the old difficulty setting was "Easy", change it to "Medium"
- For all enemy spawns (Placed NPCs), if the old difficulty setting was "Medium", change it to "Hard"
- For all enemy spawns (Placed NPCs), if the old difficulty setting was "Hard", delete the setting (force to multiplier of 1.00)
- Optionally update all Leveled NPC lists to remove the "Calculate From All Levels Less Than Or Equal Player" flag if it is set, preventing low level enemies from spawning when those lists are used

## Settings
All settings can be configured inside the Synthesis app.

### Add Skill Labels
Adds a label to any books you find that will cause you to gain skills. Dynamically pulls this information from any book as long as it has the standard 'Teaches Skill' script attached. Will not work if a custom mod is using different scripts to apply the skill up.

### Add Map Marker Labels
Adds a label to any books you find that will give you a map marker via script. Will work as long as the script is on the book itself and contains "MapMarker" (case insensitive) somehwere in the script name. Will not apply to scripts belonging to the quest record instead of the book. This may be able to be improved in the future.

#### Add Quest Labels
Adds a label to any books you find that are involved in a quest. Enabling this label will require the patcher to create a quest book cache at the beginning of each run, which will extend processing time a bit. This cache looks through all quests and finds any aliases that reference a book. If found, that book will be marked as a quest book. Also checks for scripts on the book itself that have the word "Quest" (case insenstive) in the name. Please open a [Github issue](https://github.com/Synthesis-Collective/SynBookSmart/issues) if you find a quest book that is not being caught by the patcher, even if it's from a mod.

### Label Position
- Before_Name
  - `<Alchemy> Snape's Book of Potions`
- After_Name
  - `Snape's Book of Potions <Alchemy>`

### Label Format
- Star
  - `*Snape's Book of Potions`
- Short
  - `<Alch> Snape's Book of Potions`
- Long
  - `<Alchemy> Snape's Book of Potions`

### Encapsulating Characters
This setting has no effect if a Label Format of `Star` is chosen.

- Parenthesis
  - `(Alch) Snape's Book of Potions`
- Curly Brackets
  - `{Alch} Snape's Book of Potions`
- Square Brackets
  - `[Alch] Snape's Book of Potions`
- Chevrons
  - `<Alch> Snape's Book of Potions`
  - Note that the tag will only show up in your inventory, not in the game world, if you choose this option
- Stars
  - `*Alch* Snape's Book of Potions`
