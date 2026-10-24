using System.Collections.Generic;

namespace AutoClicker
{
    public static class LanguageManager
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Translations =
            new Dictionary<string, Dictionary<string, string>>
            {
                ["Русский"] = new Dictionary<string, string>
                {
                    ["NavTitle"] = "НАВИГАЦИЯ",
                    ["Version"] = "Версия",
                    ["TabClicker"] = "Кликер",
                    ["TabTarget"] = "Цель",
                    ["TabSettings"] = "Настройки",
                    ["TabHelp"] = "Помощь",

                    ["SectionInterval"] = "Интервал между кликами",
                    ["HintInterval"] = "Выберите готовое значение или введите точное время.",
                    ["UnitHours"] = "ч",
                    ["UnitMinutes"] = "м",
                    ["UnitSeconds"] = "с",
                    ["UnitMs"] = "мс",

                    ["SectionMouse"] = "Кнопка мыши",
                    ["HintMouse"] = "Какой кнопкой кликать.",
                    ["MouseLeft"] = "Левая",
                    ["MouseRight"] = "Правая",
                    ["MouseMiddle"] = "Средняя",

                    ["SectionClick"] = "Параметры клика",
                    ["LabelHold"] = "Удержание кнопки",
                    ["HintHold"] = "Для игр: кнопка остаётся зажатой заданное число миллисекунд.",
                    ["CheckReturnCursor"] = "Возвращать курсор на место",
                    ["HintReturnCursor"] = "Удобно, если вы работаете мышью, пока кликер работает.",

                    ["SectionAdvanced"] = "Дополнительно",
                    ["HintAdvanced"] = "Циклический режим, рандомизация, лимиты.",
                    ["AdvCyclic"] = "Циклический режим: клики / отдых",
                    ["LabelClickDur"] = "Клики, сек",
                    ["LabelRestDur"] = "Отдых, сек",
                    ["AdvRandom"] = "Рандомизация координат",
                    ["HintRandom"] = "Точка клика случайно смещается внутри выбранной зоны.",
                    ["LabelRandX"] = "Разброс X, px",
                    ["LabelRandY"] = "Разброс Y, px",
                    ["AdvHuman"] = "Человеческая задержка",
                    ["HintHuman"] = "Интервал слегка «дышит», чтобы клики не были идеально ровными.",
                    ["LabelHumanPercent"] = "Отклонение, %",
                    ["AdvLimits"] = "Лимиты",
                    ["CheckClickLimit"] = "Остановить после N кликов",
                    ["LabelMaxClicks"] = "Количество",
                    ["CheckTimeLimit"] = "Остановить по времени",
                    ["LabelTimeH"] = "Ч",
                    ["LabelTimeM"] = "М",
                    ["LabelTimeS"] = "С",

                    ["SectionTarget"] = "Куда кликать",
                    ["HintTarget"] = "Зона, точка или окно — кликер работает только там.",
                    ["BtnSelectZone"] = "Выбрать зону",
                    ["BtnSelectPoint"] = "Выбрать точку",
                    ["BtnSelectWindow"] = "Выбрать окно",
                    ["SectionZoneInfo"] = "Текущая цель",
                    ["LabelZone"] = "Зона",
                    ["LabelPoint"] = "Точка",
                    ["LabelWindow"] = "Окно",
                    ["LabelWidth"] = "Ширина",
                    ["LabelHeight"] = "Высота",
                    ["LabelPosition"] = "Позиция",
                    ["TargetNone"] = "не выбрана",
                    ["TargetScreen"] = "экран",

                    ["SectionMode"] = "Режим работы",
                    ["HintMode"] = "Наведите на режим, чтобы увидеть подсказку.",
                    ["ModeForeground"] = "Обычный",
                    ["ModeForegroundDesc"] = "Клики через SendInput, как настоящая мышь. Работает везде, включая игры в фокусе.",
                    ["ModeBackground"] = "Фоновый",
                    ["ModeBackgroundDesc"] = "Клики прямо в окно, курсор не двигается. Окно может быть свёрнуто. Игры с Raw Input такое обычно игнорируют.",
                    ["ModeGame"] = "Игровой",
                    ["ModeGameDesc"] = "Активирует выбранное окно и кликает с удержанием. Для оконного или безрамочного режима игры.",

                    ["SectionTest"] = "Проверка",
                    ["BtnTestClick"] = "Тестовый клик",
                    ["HintTest"] = "Проверьте позицию и кнопку перед запуском.",

                    ["BtnStart"] = "Старт",
                    ["BtnStop"] = "Стоп",
                    ["BtnRunning"] = "Работает",

                    ["StatusReady"] = "Готов к работе",
                    ["StatusRunning"] = "Кликаю…",
                    ["StatusStopped"] = "Остановлено",
                    ["StatusLimit"] = "Остановлено: достигнут лимит кликов",
                    ["StatusTime"] = "Остановлено: истёк лимит времени",
                    ["StatusError"] = "Остановлено из-за ошибки",
                    ["StatusSelectZone"] = "Выделите зону на экране · Esc — отмена",
                    ["StatusSelectPoint"] = "Кликните по нужной точке · Esc — отмена",
                    ["StatusSelectWindow"] = "Кликните по нужному окну · 10 секунд",
                    ["StatusZoneSelected"] = "Зона выбрана",
                    ["StatusPointSelected"] = "Точка выбрана",
                    ["StatusWindowSelected"] = "Окно выбрано",
                    ["StatusHotkeyBusy"] = "Комбинация занята — выберите другую",
                    ["StatusHotkeyOff"] = "Горячая клавиша отключена",
                    ["StatusWindowMissing"] = "Сначала выберите окно во вкладке «Цель»",
                    ["StatusTest"] = "Тестовый клик",
                    ["StatusCapture"] = "Нажмите комбинацию · Esc — отмена · Delete — выключить",
                    ["StatusSaved"] = "Тема сохранена",
                    ["StatusDeleted"] = "Тема удалена",
                    ["StatusThemeApplied"] = "Тема применена",

                    ["SectionHotkey"] = "Горячая клавиша",
                    ["HintHotkey"] = "Старт и стоп из любого приложения.",
                    ["HotkeyOff"] = "Выкл.",
                    ["BtnChange"] = "Изменить",

                    ["SectionTheme"] = "Тема оформления",
                    ["HintTheme"] = "Нажмите на плитку — тема применится сразу.",
                    ["ThemeCustom"] = "Своя тема",

                    ["SectionCustomColors"] = "Свои цвета",
                    ["HintCustomColors"] = "Настройте палитру под себя: изменения видно сразу.",
                    ["CustomBackground"] = "Фон окна",
                    ["CustomForeground"] = "Текст",
                    ["CustomBorder"] = "Границы",
                    ["CustomControl"] = "Поля и карточки",
                    ["CustomButton"] = "Кнопки",
                    ["CustomAccent"] = "Акцент",
                    ["BtnPick"] = "Выбрать",

                    ["SectionInterface"] = "Интерфейс",
                    ["LabelOpacity"] = "Прозрачность",
                    ["CheckTopmost"] = "Поверх всех окон",
                    ["CheckTray"] = "Сворачивать в трей",
                    ["CheckSmooth"] = "Плавная прокрутка",

                    ["SectionLanguage"] = "Язык",
                    ["SectionProfiles"] = "Сохранённые темы",
                    ["HintProfiles"] = "Свои палитры можно сохранять и загружать.",
                    ["LabelThemeName"] = "Название",
                    ["BtnSaveTheme"] = "Сохранить",
                    ["BtnLoadTheme"] = "Загрузить",
                    ["BtnDeleteTheme"] = "Удалить",
                    ["NoSavedThemes"] = "пока пусто",

                    ["SectionAbout"] = "О программе",
                    ["AboutText"] = "AutoClicker Pro · WPF · .NET 4.7.2",
                    ["HintApp"] = "Настройки сохраняются автоматически.",
                    ["LabelClicks"] = "Кликов",
                    ["LabelCps"] = "Клик/с",
                    ["LabelUptime"] = "Время",
                    ["LabelIntervalShort"] = "Интервал",
                    ["SectionSession"] = "Статистика сессии",

                    ["HelpTitle"] = "Как это работает",
                    ["HelpIntro"] = "Четыре шага — и кликер делает свою работу.",
                    ["HelpStep1Title"] = "1. Выберите цель",
                    ["HelpStep1Desc"] = "Вкладка «Цель»: «Выбрать зону» — область на экране, «Выбрать точку» — одиночное место, «Выбрать окно» — для фонового и игрового режимов.",
                    ["HelpStep2Title"] = "2. Настройте клики",
                    ["HelpStep2Desc"] = "Интервал, кнопка мыши, режим. Для игр включите «Игровой» режим и удержание 15–50 мс.",
                    ["HelpStep3Title"] = "3. Назначьте горячую клавишу",
                    ["HelpStep3Desc"] = "Нажмите на клавишу в шапке и выберите комбинацию — она сработает из любого приложения.",
                    ["HelpStep4Title"] = "4. Запускайте",
                    ["HelpStep4Desc"] = "Кнопка «Старт» или горячая клавиша. «Стоп» останавливает мгновенно.",
                    ["HelpGamesTitle"] = "Про игры",
                    ["HelpGamesDesc"] = "Игровой режим использует SendInput и активацию окна. В эксклюзивном полноэкранном режиме игра может блокировать чужие клики — переключите её в оконный или безрамочный режим. Фоновый режим (PostMessage) игры на Raw Input игнорируют.",
                    ["HelpAcTitle"] = "Античит",
                    ["HelpAcDesc"] = "В онлайн-играх автоматизация запрещена правилами и может привести к блокировке аккаунта. Используйте на свой риск.",

                    ["TrayShow"] = "Открыть",
                    ["TrayExit"] = "Выход",
                    ["TrayTooltip"] = "AutoClicker Pro",

                    ["OverlayZone"] = "Зажмите левую кнопку мыши и выделите область",
                    ["OverlayPoint"] = "Кликните по точке, куда нужно кликать",
                    ["OverlayWindow"] = "Кликните по окну, в которое нужно кликать",
                    ["OverlayCancel"] = "Esc — отмена",
                    ["OverlayWindowCancel"] = "Подождите 10 секунд или выберите окно кликом",

                    ["MsgConfirmDelete"] = "Удалить эту тему?",
                    ["ToastSaved"] = "Сохранено"
                },

                ["English"] = new Dictionary<string, string>
                {
                    ["NavTitle"] = "NAVIGATION",
                    ["Version"] = "Version",
                    ["TabClicker"] = "Clicker",
                    ["TabTarget"] = "Target",
                    ["TabSettings"] = "Settings",
                    ["TabHelp"] = "Help",

                    ["SectionInterval"] = "Click interval",
                    ["HintInterval"] = "Pick a preset or enter an exact value.",
                    ["UnitHours"] = "h",
                    ["UnitMinutes"] = "m",
                    ["UnitSeconds"] = "s",
                    ["UnitMs"] = "ms",

                    ["SectionMouse"] = "Mouse button",
                    ["HintMouse"] = "Which button to click with.",
                    ["MouseLeft"] = "Left",
                    ["MouseRight"] = "Right",
                    ["MouseMiddle"] = "Middle",

                    ["SectionClick"] = "Click options",
                    ["LabelHold"] = "Button hold",
                    ["HintHold"] = "For games: keep the button pressed for this many milliseconds.",
                    ["CheckReturnCursor"] = "Restore cursor position",
                    ["HintReturnCursor"] = "Handy if you keep using the mouse while clicking.",

                    ["SectionAdvanced"] = "Advanced",
                    ["HintAdvanced"] = "Cyclic mode, randomization, limits.",
                    ["AdvCyclic"] = "Cyclic mode: click / rest",
                    ["LabelClickDur"] = "Click, sec",
                    ["LabelRestDur"] = "Rest, sec",
                    ["AdvRandom"] = "Coordinate randomization",
                    ["HintRandom"] = "The click point jitters inside the selected zone.",
                    ["LabelRandX"] = "Spread X, px",
                    ["LabelRandY"] = "Spread Y, px",
                    ["AdvHuman"] = "Human-like delay",
                    ["HintHuman"] = "The interval slightly breathes so clicks are not perfectly even.",
                    ["LabelHumanPercent"] = "Deviation, %",
                    ["AdvLimits"] = "Limits",
                    ["CheckClickLimit"] = "Stop after N clicks",
                    ["LabelMaxClicks"] = "Clicks",
                    ["CheckTimeLimit"] = "Stop after time",
                    ["LabelTimeH"] = "H",
                    ["LabelTimeM"] = "M",
                    ["LabelTimeS"] = "S",

                    ["SectionTarget"] = "Where to click",
                    ["HintTarget"] = "A zone, a point or a window — the clicker works only there.",
                    ["BtnSelectZone"] = "Select zone",
                    ["BtnSelectPoint"] = "Select point",
                    ["BtnSelectWindow"] = "Select window",
                    ["SectionZoneInfo"] = "Current target",
                    ["LabelZone"] = "Zone",
                    ["LabelPoint"] = "Point",
                    ["LabelWindow"] = "Window",
                    ["LabelWidth"] = "Width",
                    ["LabelHeight"] = "Height",
                    ["LabelPosition"] = "Position",
                    ["TargetNone"] = "not selected",
                    ["TargetScreen"] = "screen",

                    ["SectionMode"] = "Operation mode",
                    ["HintMode"] = "Hover a mode to read its hint.",
                    ["ModeForeground"] = "Normal",
                    ["ModeForegroundDesc"] = "SendInput clicks, just like a real mouse. Works everywhere, including games in focus.",
                    ["ModeBackground"] = "Background",
                    ["ModeBackgroundDesc"] = "Clicks straight into the window, cursor stays put. The window may be minimized. Raw-input games ignore this.",
                    ["ModeGame"] = "Game",
                    ["ModeGameDesc"] = "Activates the selected window and clicks with a hold. For windowed or borderless games.",

                    ["SectionTest"] = "Test",
                    ["BtnTestClick"] = "Test click",
                    ["HintTest"] = "Check position and button before starting.",

                    ["BtnStart"] = "Start",
                    ["BtnStop"] = "Stop",
                    ["BtnRunning"] = "Running",

                    ["StatusReady"] = "Ready",
                    ["StatusRunning"] = "Clicking…",
                    ["StatusStopped"] = "Stopped",
                    ["StatusLimit"] = "Stopped: click limit reached",
                    ["StatusTime"] = "Stopped: time limit reached",
                    ["StatusError"] = "Stopped due to an error",
                    ["StatusSelectZone"] = "Drag a zone on screen · Esc to cancel",
                    ["StatusSelectPoint"] = "Click the point you need · Esc to cancel",
                    ["StatusSelectWindow"] = "Click the target window · 10 seconds",
                    ["StatusZoneSelected"] = "Zone selected",
                    ["StatusPointSelected"] = "Point selected",
                    ["StatusWindowSelected"] = "Window selected",
                    ["StatusHotkeyBusy"] = "Shortcut is taken — pick another one",
                    ["StatusHotkeyOff"] = "Hotkey disabled",
                    ["StatusWindowMissing"] = "Select a window on the Target tab first",
                    ["StatusTest"] = "Test click",
                    ["StatusCapture"] = "Press a shortcut · Esc to cancel · Delete to disable",
                    ["StatusSaved"] = "Theme saved",
                    ["StatusDeleted"] = "Theme deleted",
                    ["StatusThemeApplied"] = "Theme applied",

                    ["SectionHotkey"] = "Hotkey",
                    ["HintHotkey"] = "Start and stop from any app.",
                    ["HotkeyOff"] = "Off",
                    ["BtnChange"] = "Change",

                    ["SectionTheme"] = "Color theme",
                    ["HintTheme"] = "Click a tile — the theme applies instantly.",
                    ["ThemeCustom"] = "Custom",

                    ["SectionCustomColors"] = "Custom colors",
                    ["HintCustomColors"] = "Tune the palette: changes apply live.",
                    ["CustomBackground"] = "Window background",
                    ["CustomForeground"] = "Text",
                    ["CustomBorder"] = "Borders",
                    ["CustomControl"] = "Fields & cards",
                    ["CustomButton"] = "Buttons",
                    ["CustomAccent"] = "Accent",
                    ["BtnPick"] = "Pick",

                    ["SectionInterface"] = "Interface",
                    ["LabelOpacity"] = "Opacity",
                    ["CheckTopmost"] = "Always on top",
                    ["CheckTray"] = "Minimize to tray",
                    ["CheckSmooth"] = "Smooth scrolling",

                    ["SectionLanguage"] = "Language",
                    ["SectionProfiles"] = "Saved themes",
                    ["HintProfiles"] = "Save your palettes and switch between them.",
                    ["LabelThemeName"] = "Name",
                    ["BtnSaveTheme"] = "Save",
                    ["BtnLoadTheme"] = "Load",
                    ["BtnDeleteTheme"] = "Delete",
                    ["NoSavedThemes"] = "empty for now",

                    ["SectionAbout"] = "About",
                    ["AboutText"] = "AutoClicker Pro · WPF · .NET 4.7.2",
                    ["HintApp"] = "Settings are saved automatically.",
                    ["LabelClicks"] = "Clicks",
                    ["LabelCps"] = "Clicks/s",
                    ["LabelUptime"] = "Uptime",
                    ["LabelIntervalShort"] = "Interval",
                    ["SectionSession"] = "Session stats",

                    ["HelpTitle"] = "How it works",
                    ["HelpIntro"] = "Four steps and the clicker does the job.",
                    ["HelpStep1Title"] = "1. Pick a target",
                    ["HelpStep1Desc"] = "Target tab: ‘Select zone’ for an area, ‘Select point’ for a single spot, ‘Select window’ for background and game modes.",
                    ["HelpStep2Title"] = "2. Tune the clicks",
                    ["HelpStep2Desc"] = "Interval, mouse button, mode. For games enable the Game mode and a 15–50 ms hold.",
                    ["HelpStep3Title"] = "3. Set a hotkey",
                    ["HelpStep3Desc"] = "Click the key chip in the header and choose a combination — it works from any app.",
                    ["HelpStep4Title"] = "4. Run it",
                    ["HelpStep4Desc"] = "Press Start or the hotkey. Stop halts instantly.",
                    ["HelpGamesTitle"] = "About games",
                    ["HelpGamesDesc"] = "Game mode uses SendInput and window activation. In exclusive fullscreen the game may block external clicks — switch it to windowed or borderless. Raw-input games ignore the background PostMessage mode.",
                    ["HelpAcTitle"] = "Anti-cheat",
                    ["HelpAcDesc"] = "Automation is against the rules in online games and may lead to an account ban. Use at your own risk.",

                    ["TrayShow"] = "Open",
                    ["TrayExit"] = "Exit",
                    ["TrayTooltip"] = "AutoClicker Pro",

                    ["OverlayZone"] = "Hold the left mouse button and drag a zone",
                    ["OverlayPoint"] = "Click the point where to click",
                    ["OverlayWindow"] = "Click the window to click into",
                    ["OverlayCancel"] = "Esc to cancel",
                    ["OverlayWindowCancel"] = "Wait 10 seconds or click the window",

                    ["MsgConfirmDelete"] = "Delete this theme?",
                    ["ToastSaved"] = "Saved"
                }
            };

        public static string[] AvailableLanguages => new[] { "Русский", "English" };

        public static string GetString(string language, string key)
        {
            if (language != null && Translations.TryGetValue(language, out var dict) && dict.TryGetValue(key, out var value))
                return value;

            if (Translations.TryGetValue("English", out var fallback) && fallback.TryGetValue(key, out var en))
                return en;

            return key;
        }

        public static bool IsSupported(string language)
            => language != null && Translations.ContainsKey(language);
    }
}
