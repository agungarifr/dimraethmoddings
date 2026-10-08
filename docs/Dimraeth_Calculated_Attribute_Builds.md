# Dimraeth - Exact Verified Attribute Builds
Based on decompiled ISIL analysis of GameAssembly.dll.

## Mechanics Verified via ISIL Assembly
The game uses the following logic unconditionally for calculating combat power and upgrade costs.
1. **Physical Combat Power**: STR + 0.5 * (AGI + ADV)
2. **Magic Combat Power**: INT + 0.5 * (CHA + ADV)
   - *Note: Agility offers 0 scaling for Magic for all races, debunking the 'Elf AGI passive' myth.*
3. **Upgrade Cost**: BaseCost * (1 + (pointsAboveStart - 1) / 3)^1.1 + (PlayerLevel * 100)

Below are the exact Level 25 stat distributions formatted as [Memory, Charisma, Adventure, Physique, Intelligence, Agility, Strength, Energy].

## 1. Human Magician
* **Best Physical:** [7, 7, 14, 5, 8, 7, 38, 6] -> **Patk 48.5** | Matk 18.5
* **Best Magic:** [7, 10, 9, 5, 49, 5, 4, 6] -> Patk 11.0 | **Matk 58.5**
* **Best Hybrid (Balanced):** [7, 8, 26, 5, 22, 8, 22, 6] -> **Patk 39.0 | Matk 39.0**

## 2. Human Brawler
* **Best Physical:** [5, 5, 9, 7, 4, 9, 49, 7] -> **Patk 58.0** | Matk 11.0
* **Best Magic:** [5, 7, 14, 7, 38, 6, 8, 7] -> Patk 18.0 | **Matk 48.5**
* **Best Hybrid (Balanced):** [5, 7, 28, 7, 21, 6, 22, 7] -> **Patk 39.0 | Matk 38.5**

## 3. Human Shadow
* **Best Physical:** [6, 6, 12, 4, 6, 13, 41, 6] -> **Patk 53.5** | Matk 15.0
* **Best Magic:** [6, 12, 13, 4, 41, 8, 6, 6] -> Patk 16.5 | **Matk 53.5**
* **Best Hybrid (Balanced):** [6, 8, 27, 4, 22, 10, 21, 6] -> **Patk 39.5 | Matk 39.5**

## 4. Elf Magician
* **Best Physical:** [7, 7, 15, 3, 8, 16, 34, 6] -> **Patk 49.5** | Matk 19.0
* **Best Magic:** [7, 7, 11, 3, 51, 7, 4, 6] -> Patk 13.0 | **Matk 60.0**
* **Best Hybrid (Balanced):** [7, 7, 25, 3, 24, 8, 23, 6] -> **Patk 39.5 | Matk 40.0**

## 5. Elf Brawler
* **Best Physical:** [5, 5, 11, 5, 4, 15, 45, 7] -> **Patk 58.0** | Matk 12.0
* **Best Magic:** [5, 8, 13, 5, 39, 8, 8, 7] -> Patk 18.5 | **Matk 49.5**
* **Best Hybrid (Balanced):** [5, 6, 28, 5, 22, 9, 21, 7] -> **Patk 39.5 | Matk 39.0**

## 6. Elf Shadow
* **Best Physical:** [6, 6, 10, 2, 6, 18, 40, 6] -> **Patk 54.0** | Matk 14.0
* **Best Magic:** [6, 8, 11, 2, 45, 10, 6, 6] -> Patk 16.5 | **Matk 54.5**
* **Best Hybrid (Balanced):** [6, 7, 29, 2, 22, 13, 19, 6] -> **Patk 40.0 | Matk 40.0**

## 7. Minotaur Magician
* **Best Physical:** [6, 6, 12, 6, 6, 7, 43, 7] -> **Patk 52.5** | Matk 15.0
* **Best Magic:** [6, 9, 9, 6, 47, 5, 6, 7] -> Patk 13.0 | **Matk 56.0**
* **Best Hybrid (Balanced):** [6, 6, 27, 6, 23, 6, 23, 7] -> **Patk 39.5 | Matk 39.5**

## 8. Minotaur Brawler
* **Best Physical:** [4, 4, 9, 8, 2, 6, 56, 8] -> **Patk 63.5** | Matk 8.5
* **Best Magic:** [4, 6, 14, 8, 36, 6, 10, 8] -> Patk 20.0 | **Matk 46.0**
* **Best Hybrid (Balanced):** [4, 5, 26, 8, 23, 6, 23, 8] -> **Patk 39.0 | Matk 38.5**

## 9. Minotaur Shadow
* **Best Physical:** [5, 5, 11, 5, 4, 10, 47, 7] -> **Patk 57.5** | Matk 12.0
* **Best Magic:** [5, 7, 12, 5, 41, 8, 8, 7] -> Patk 18.0 | **Matk 50.5**
* **Best Hybrid (Balanced):** [5, 6, 27, 5, 23, 10, 21, 7] -> **Patk 39.5 | Matk 39.5**
