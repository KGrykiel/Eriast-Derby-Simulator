# 🚧 WORK IN PROGRESS 🚧

# Eriast-Derby-Simulator

A simulator for a homebrew DnD module centered around vehicle racing using a framework of turn-based mechanics.

The main idea behind it is that a group of players operate one vehicle and try to get around a circuit denoted by discrete stages faster than their opponents. Combat is not only allowed but also encouraged. The behavior of other vehicles will be simulated for additional realism and more interesting gameplay.

It is being built with modularity and extensibility in mind as more and more features are being workshopped by our creative team (me and my buddy Bence). This project uses the Unity Engine and its main repository is within Unity's inbuilt version control. This GitHub project is a copy of the scripts for showcase purposes.

---

## Current Features

- **Attribute and Modifier System**  
  Keeping track of all stats of the vehicles as well as dynamically applied modifiers supporting both flat and percentage changes. Vehicles and other entities can be created directly in the editor.

- **Effect System**  
  A unified way of handling all kinds of common actions that happen in a DnD setting such as dealing damage, applying buffs, draining resources, etc. This allows code reuse across multiple sources of such actions like player skills or environmental effects (e.g., from event cards). It is also very easy to add different types of effects if need arises.

- **Skill System**  
  Skills can be created as ScriptableObjects and assigned to vehicles in a similar manner as spells in DnD-style RPGs. The system allows for any kind of combination of effects including custom ones. Creating new skills can be done directly from the editor.

- **Event Card System**  
  Part of the homebrew mechanics devised for the purposes of this module. A more classic combat with a gridmap as used in DnD turned out to be boring to players during playtesting and therefore it is being replaced by a system where challenges presented by each stage of the track are abstracted by a set of event cards that are drawn at the beginning of every turn and can provide bonuses, introduce a problem to overcome, or require players to make a choice. It has also been built with modularity in mind to accommodate addition of more unique situations.

- **Turn Management**  
  A simple system controlling the progression of the race, taking into account the position of each vehicle and its sphere of influence at any given point (which entities they can interact with).

- **UI**  
  A rudimentary UI in Unity used for debugging purposes. Not shown as it was not made to be pretty :)

- **Logging and Monitoring**  
  A subsystem for tracking the situation at any given point. Includes a monitor of statistics for every vehicle as well as a log describing everything that happened. Used for debugging.

---

## Features in Progress

- Vehicle AI.
- Division of a vehicle into parts and roles (e.g., Driver, Gunner, Navigator, Technician) each with their own set of skills.
- Common resource management system for the whole vehicle.
- Vehicle building and advanced attributes that are derived from the parts used.
- ...and more!
