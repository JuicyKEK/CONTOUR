# Звуки монстров (CC0)

Скачаны 2026-11-20 с OpenGameArt по выбору владельца «Скачать бесплатные CC0». У каждого набора на странице указана лицензия CC0 (общественное достояние): можно использовать в игре и менять, указывать автора не обязательно.

| Папка | Набор | Автор | Страница |
|---|---|---|---|
| CC0_rubberduck_80_creature_sfx | 80 CC0 creature SFX | rubberduck | https://opengameart.org/content/80-cc0-creature-sfx |
| CC0_rubberduck_80_creature_sfx_2 | 80 CC0 creture SFX #2 | rubberduck | https://opengameart.org/content/80-cc0-creture-sfx-2 |
| CC0_darsycho_monster_snarls | Monster snarls | darsycho | https://opengameart.org/content/monster-snarls |
| CC0_darsycho_troll_sounds | Big scary troll sounds (troll-roars) | darsycho | https://opengameart.org/content/big-scary-troll-sounds |
| CC0_ezduzziteh_alien_sounds | Alien Sounds (alien_hiss) | ezduzziteh | https://opengameart.org/node/183316 |
| CC0_trazzz123_deep_monster_roar | CC0 Deep Monster Roar | trazzz123 | https://opengameart.org/content/cc0-deep-monster-roar |

Что стоит в профилях монстров комплекса (`Assets/Game/SO/NPC/HousingComplex/<монстр>/NpcAnimationSoundProfile.asset`). Подобрано по замерам, а не на слух: шипение — короткие шумовые звуки с высокими частотами, рык — низкие.

| Монстр | Шипение (голос состояния Threaten) | Рык (ключ Roar) | В погоне (голос Chase) |
|---|---|---|---|
| Dorm_04_CurlersWoman | alien_02, attack_02 | monster_20 | monster_04, monster_07, monster_12 |
| Flat5Bathtub | monster_05, alien_03 | monster_roar (trazzz123) | monster_04, monster_07, monster_12 |
| Flat10Scraper | alien_06, misc_14 | roar_05 | monster_04, monster_07, monster_12 |
| KitchenCrawler | alien_08, monster_01 | roar_04 | monster_04, monster_07, monster_12 |

Заменить звук: в профиле монстра поле Clips у голоса Threaten (шипение) или Chase, либо поле Sound у записи с ключом Roar.
