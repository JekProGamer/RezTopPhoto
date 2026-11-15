# RezTopPhoto Billboard — CS2 asset preparation

Исходники широкого рекламного борда RezTop.cc и первый материал Source 2.

## Файлы
- `reztop_billboard.obj` — геометрия борда в формате OBJ.
- `reztop_billboard.mtl` — материалы для предпросмотра OBJ.
- `reztop_billboard_texture.png` — оригинальный баннер.
- `reztop_billboard.vmat` — исходник материала CS2 на шейдере `csgo_complex.vfx`.
- `compile_material.bat` — помощник для копирования исходников в addon и запуска Resource Compiler.

## Этап 1. Подготовка материала и текстуры

1. Скачай всю папку `RezTopBillboard` из репозитория.
2. Проверь путь установки CS2 в файле `compile_material.bat`. По умолчанию указан стандартный путь Steam.
3. Запусти `compile_material.bat`.
4. Скрипт скопирует VMAT и PNG в:

```
Counter-Strike Global Offensive/
└── content/csgo_addons/reztopphoto/
    └── materials/reztopphoto/
        ├── reztop_billboard.vmat
        └── reztop_billboard_texture.png
```

Затем скрипт запустит `game/bin/win64/resourcecompiler.exe` для VMAT. Проверь вывод компилятора и наличие скомпилированных ресурсов в папке addon. Если CS2 установлен не в стандартную папку, исправь `CS2_ROOT` в BAT-файле.

В VMAT указан путь к текстуре:

```
materials/reztopphoto/reztop_billboard_texture.png
```

## Этап 2. Модель

OBJ — это промежуточный формат, а не готовая игровая модель Source 2.

1. Импортируй `reztop_billboard.obj` в Blender.
2. Проверь UV-развёртку и слоты материалов `Photo_Surface` и `Frame_Black`.
3. Назначь материал баннера лицевой поверхности, а рамке — тёмный материал.
4. Экспортируй геометрию в FBX.
5. Открой CS2 Workshop Tools → ModelDoc, создай модель, импортируй FBX и назначь скомпилированный материал борда.
6. Настрой коллизию/флаги модели по необходимости и скомпилируй `.vmdl`.

## Статус
- [x] PNG баннера добавлен в репозиторий.
- [x] OBJ/MTL исходники добавлены.
- [x] VMAT для CS2 добавлен.
- [x] BAT-помощник для копирования исходников и запуска компилятора добавлен.
- [ ] Материал и текстура успешно скомпилированы и проверены в CS2 Workshop Tools.
- [ ] Геометрия импортирована в ModelDoc и скомпилирована в `.vmdl_c`.
- [ ] Борд проверен на игровой карте.

Важно: CounterStrikeSharp не превращает произвольный PNG в игровую 3D-текстуру только по пути к файлу. Модель, материал и текстура должны быть подготовлены и доступны клиентам через поддерживаемый механизм распространения ресурсов.
