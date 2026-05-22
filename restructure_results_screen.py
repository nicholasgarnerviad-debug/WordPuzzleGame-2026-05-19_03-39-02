#!/usr/bin/env python3
"""
Restructure ResultsScreen in GameUI.unity to match specification.

This script:
1. Renames existing elements to match spec names
2. Creates StatsList as ScrollRect with VerticalLayoutGroup content
3. Creates 7 stat items
4. Creates ButtonContainer with HorizontalLayoutGroup
5. Updates ResultsScreen.cs field references
"""

import re
import sys
from typing import Dict, Tuple

# Starting FileID for new components
BASE_FILE_ID = 2116810006

class SceneRestructurer:
    def __init__(self, scene_path: str):
        self.scene_path = scene_path
        with open(scene_path, 'r', encoding='utf-8') as f:
            self.content = f.read()
        self.next_id = BASE_FILE_ID
        self.new_objects: Dict[str, int] = {}

    def get_new_id(self) -> int:
        """Get next available fileID"""
        id_val = self.next_id
        self.next_id += 1
        return id_val

    def rename_gameobject(self, old_name: str, new_name: str):
        """Rename a GameObject in the scene"""
        # Find the m_Name field for this object
        pattern = rf"(  m_Name: ){re.escape(old_name)}(\n)"
        replacement = rf"\1{new_name}\2"
        self.content = re.sub(pattern, replacement, self.content)
        print(f"Renamed {old_name} -> {new_name}")

    def save(self):
        """Save the modified scene"""
        with open(self.scene_path, 'w', encoding='utf-8') as f:
            f.write(self.content)
        print(f"Saved to {self.scene_path}")

def main():
    scene_path = r'C:\Dev\WordPuzzleGame\Assets\Scenes\GameUI.unity'

    restructurer = SceneRestructurer(scene_path)

    # Step 1: Rename existing GameObjects
    restructurer.rename_gameobject("ModeNameText", "HeaderText")
    restructurer.rename_gameobject("ScoreText", "FinalScoreText")
    restructurer.rename_gameobject("NextButton", "PlayAgainButton")
    restructurer.rename_gameobject("MenuButton", "MainMenuButton")
    restructurer.rename_gameobject("CoinsEarnedText", "FinalScoreStat")
    restructurer.rename_gameobject("TimeText", "DurationStat")

    # For now, just save these renames
    restructurer.save()

    print("\nPhase 1 complete: Renamed elements")
    print("Next steps:")
    print("  - Create StatsList ScrollRect with StatsContent")
    print("  - Create 5 new stat item GameObjects")
    print("  - Create ButtonContainer and move buttons")
    print("  - Update ResultsScreen.cs field references in inspector")

if __name__ == '__main__':
    main()
