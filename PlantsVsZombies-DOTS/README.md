# Plants vs. Zombies - DOTS

## Overview
This project is a simple implementation of a Plants vs. Zombies game using Unity's Data-Oriented Technology Stack (DOTS). The game features various plants and zombies, each with unique properties and behaviors.

## Project Structure
- **Assets/Scripts/Components**: Contains the data components for plants, zombies, projectiles, and health management.
- **Assets/Scripts/Systems**: Includes systems that handle the game logic, such as spawning plants, moving zombies, combat interactions, and projectile behavior.
- **Assets/Scripts/Authoring**: Responsible for converting GameObjects into entities with the appropriate components.
- **Assets/Scripts/Data**: Contains configuration settings for the game.
- **Assets/Scenes**: The main game scene where the gameplay takes place.
- **Assets/Prefabs**: Contains prefabs for different types of plants and zombies.
- **Packages**: Contains the manifest file for Unity package dependencies.
- **ProjectSettings**: Contains project version information.

## Setup Instructions
1. Open the project in Unity.
2. Ensure that the necessary DOTS packages are installed via the Package Manager.
3. Open the `MainGame.unity` scene to start the game.
4. Customize the game by modifying the components and systems as needed.

## Game Mechanics
- Players can plant various types of plants to defend against waves of zombies.
- Each plant has specific attributes such as damage and range.
- Zombies move towards the plants and can inflict damage if they reach them.
- Projectiles are fired by plants to damage zombies from a distance.

## Future Enhancements
- Add more plant and zombie types with unique abilities.
- Implement a scoring system and levels.
- Enhance graphics and animations for a better user experience.

## Acknowledgments
This project utilizes Unity's DOTS for efficient data management and performance optimization.