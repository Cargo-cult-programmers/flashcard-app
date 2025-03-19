using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Text;

namespace FlashcardApp
{
    public sealed partial class MainWindow : Window
    {
        private const string flashCardsFile = "flashcards.json";
        // Теперь используется только один файл для групп дисциплин

        // Вместо списка карточек и списка дисциплин – один список групп
        private List<DisciplineGroup> disciplineGroups = new List<DisciplineGroup>();

        // Для тестовой логики (тестовый пул – плоский список карточек)
        private List<FlashCard> testCardPool = new List<FlashCard>();

        private FlashCard? currentFlashCard = null;

        object? FilterDisciplineComboBox__lastSelected = null;

        public MainWindow()
        {
            this.InitializeComponent();
            SystemBackdrop = new DesktopAcrylicBackdrop();
            (this.Content as Grid)!.Background = null;
            LoadData();
        }

        #region Data I/O

        // Универсальная загрузка данных из файла
        private async Task<T?> LoadFromFile<T>(string filename)
        {
            string path = Path.Combine(ApplicationData.Current.LocalFolder.Path, filename);
            if (File.Exists(path))
            {
                string json = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<T>(json);
            }
            return default;
        }

        // Универсальное сохранение данных в файл
        private async Task SaveToFile<T>(T data, string filename)
        {
            string json = JsonSerializer.Serialize(data);
            string path = Path.Combine(ApplicationData.Current.LocalFolder.Path, filename);
            await File.WriteAllTextAsync(path, json);
        }

        // Загрузка данных: теперь загружается список групп дисциплин
        private async void LoadData()
        {
            disciplineGroups = await LoadFromFile<List<DisciplineGroup>>(flashCardsFile) ?? new List<DisciplineGroup>();
            UpdateUI();
        }

        // Сохранение данных и обновление UI
        private async Task SaveData()
        {
            await SaveToFile(disciplineGroups, flashCardsFile);
            UpdateUI();
        }

        #endregion

        #region UI Updates

        // Обновление всех элементов UI
        private void UpdateUI()
        {
            UpdateDisciplineUI();
            if (FilterDisciplineComboBox__lastSelected is null)
            {
                UpdateFlashCardsStackPanel();
            }
        }

        // Обновление списков дисциплин для всех контролов
        private void UpdateDisciplineUI()
        {
            // Сохраняем выбранное значение для вкладки "Редактирование"
            string? previouslySelected = DisciplineComboBox.SelectedItem as string;

            var disciplineNames = disciplineGroups.Select(g => g.Discipline).ToList();

            // Обновляем DisciplineComboBox
            DisciplineComboBox.ItemsSource = null;
            DisciplineComboBox.ItemsSource = disciplineNames;

            // Восстанавливаем выбранное значение, если оно есть в списке
            if (!string.IsNullOrEmpty(previouslySelected) && disciplineNames.Contains(previouslySelected))
            {
                DisciplineComboBox.SelectedItem = previouslySelected;
            }

            // Обновление для FilterDisciplineComboBox
            FilterDisciplineComboBox.Items.Clear();
            FilterDisciplineComboBox.Items.Add("Все");
            foreach (var d in disciplineNames)
            {
                FilterDisciplineComboBox.Items.Add(d);
            }

            if (FilterDisciplineComboBox__lastSelected is null)
            {
                FilterDisciplineComboBox.SelectedIndex = 0;
            }
            else
            {
                FilterDisciplineComboBox.SelectedItem = FilterDisciplineComboBox__lastSelected;
            }

            // Обновление для TestDisciplineComboBox
            if (TestDisciplineComboBox != null)
            {
                TestDisciplineComboBox.Items.Clear();
                TestDisciplineComboBox.Items.Add("Все");
                foreach (var d in disciplineNames)
                {
                    TestDisciplineComboBox.Items.Add(d);
                }
            }

            // Обновляем DisciplinesListView, указывая источник данных
            DisciplinesListView.ItemsSource = disciplineNames;
        }

        // Обновление StackPanel для карточек (просмотр/редактирование)
        private void UpdateFlashCardsStackPanel()
        {
            if (FilterDisciplineComboBox__lastSelected is null || !FilterDisciplineComboBox.Items.Contains(FilterDisciplineComboBox.SelectedValue))
            {
                FilterDisciplineComboBox__lastSelected = (FilterDisciplineComboBox.SelectedValue as string)?.ToString();
            }

            FilterDisciplineComboBox.SelectedValue = FilterDisciplineComboBox__lastSelected;

            FlashCardsStackPanel.Children.Clear();
            // Для каждой группы выводим заголовок дисциплины и её карточки
            var FilterDisciplineComboBox__lastSelectedAsString = FilterDisciplineComboBox__lastSelected as string;

            foreach (var group in disciplineGroups)
            {
                if (FilterDisciplineComboBox__lastSelectedAsString is not null
                    && FilterDisciplineComboBox__lastSelectedAsString != "Все"
                    && FilterDisciplineComboBox__lastSelectedAsString != group.Discipline)
                {
                    continue;
                }
                var header = new TextBlock
                {
                    Text = group.Discipline,
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 5),
                    Foreground = (SolidColorBrush)Application.Current.Resources["AccentAAFillColorDefaultBrush"]
                };
                FlashCardsStackPanel.Children.Add(header);

                foreach (var card in group.Cards)
                {
                    var cardUI = CreateFlashCardPanel(card, group.Discipline);
                    cardUI.DataContext = card;
                    FlashCardsStackPanel.Children.Add(cardUI);
                }
            }
        }

        // Создание UI-элемента для отдельной карточки. Дисциплина передаётся отдельно.
        private Border CreateFlashCardPanel(FlashCard card, string discipline)
        {
            var border = new Border
            {
                Background = (AcrylicBrush)Application.Current.Resources["AcrylicInAppFillColorBaseBrush"],
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 5 };

            // В карточке не дублируем название дисциплины, оно указано в заголовке группы
            stack.Children.Add(new TextBlock { Text = card.Question, TextWrapping = TextWrapping.Wrap, FontSize = 18, FontWeight = new FontWeight(600) });
            stack.Children.Add(new TextBlock { Text = card.Answer, TextWrapping = TextWrapping.Wrap, FontSize = 16 });

            var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            buttonsPanel.Children.Add(CreateButton("Редактировать", Symbol.Edit, (s, e) => EditFlashCard(card, discipline)));
            buttonsPanel.Children.Add(CreateButton("Удалить", Symbol.Delete, async (s, e) =>
            {
                DeleteFlashCard(card, discipline);
                await SaveData();
            }));
            stack.Children.Add(buttonsPanel);

            border.Child = stack;
            return border;
        }

        // Вспомогательный метод для создания кнопки с иконкой и текстом
        private Button CreateButton(string text, Symbol symbol, RoutedEventHandler handler)
        {
            var button = new Button();
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new SymbolIcon { Symbol = symbol });
            sp.Children.Add(new TextBlock { Text = text, Margin = new Thickness(5, 0, 0, 0) });
            button.Content = sp;
            button.Click += handler;
            return button;
        }

        #endregion

        #region Flashcard Editing and Discipline Management

        // Сохранение новой карточки (вкладка "Редактирование")
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string? discipline = "";
            // Если введена новая дисциплина, используем её
            if (!string.IsNullOrEmpty(NewDisciplineTextBox.Text))
            {
                discipline = NewDisciplineTextBox.Text.Trim();
                // Если такой группы нет – создаём новую
                if (!disciplineGroups.Any(g => g.Discipline == discipline))
                {
                    disciplineGroups.Add(new DisciplineGroup { Discipline = discipline });
                }
            }
            else if (DisciplineComboBox.SelectedItem != null)
            {
                discipline = DisciplineComboBox.SelectedItem.ToString();
            }
            else
            {
                var dialog = new ContentDialog
                {
                    Title = "Ошибка",
                    Content = "Выберите или введите дисциплину.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot,
                };
                _ = dialog.ShowAsync();
                return;
            }

            string question = QuestionTextBox.Text.Trim();
            string answer = AnswerTextBox.Text.Trim();

            if (!string.IsNullOrEmpty(question) && !string.IsNullOrEmpty(answer))
            {
                // Находим группу или создаём новую
                var group = disciplineGroups.FirstOrDefault(g => g.Discipline == discipline);
                if (group == null && discipline is not null)
                {
                    group = new DisciplineGroup { Discipline = discipline };
                    disciplineGroups.Add(group);
                }
                group?.Cards.Add(new FlashCard { Question = question.Replace("\t", " "), Answer = answer.Replace("\t", " ") });
                await SaveData();

                // Очищаем поля, но выбранная дисциплина остается (если выбрана из списка)
                QuestionTextBox.Text = "";
                AnswerTextBox.Text = "";
                NewDisciplineTextBox.Text = "";
            }
        }

        // Добавление новой дисциплины (вкладка "Редактирование")
        private async void AddDisciplineButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(NewDisciplineTextBox.Text))
            {
                string newDisc = NewDisciplineTextBox.Text.Trim();
                if (!disciplineGroups.Any(g => g.Discipline == newDisc))
                {
                    disciplineGroups.Add(new DisciplineGroup { Discipline = newDisc });
                    await SaveData();
                    NewDisciplineTextBox.Text = "";
                }
                else
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Ошибка",
                        Content = "Дисциплина с таким названием уже существует.",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot,
                    };
                    _ = dialog.ShowAsync();
                }
            }
        }

        // Добавление новой дисциплины (вкладка "Дисциплины")
        private async void AddDisciplineButton2_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(NewDisciplineTextBox2.Text))
            {
                string newDisc = NewDisciplineTextBox2.Text.Trim();
                if (!disciplineGroups.Any(g => g.Discipline == newDisc))
                {
                    disciplineGroups.Add(new DisciplineGroup { Discipline = newDisc });
                    await SaveData();
                    NewDisciplineTextBox2.Text = "";
                }
                else
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Ошибка",
                        Content = "Дисциплина с таким названием уже существует.",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot,
                    };
                    _ = dialog.ShowAsync();
                }
            }
        }

        // Удаление дисциплины (вкладка "Дисциплины")
        private async void DeleteDisciplineButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is string discipline)
            {
                var confirmDialog = new ContentDialog
                {
                    Title = "Подтверждение удаления",
                    Content = $"Удалить дисциплину \"{discipline}\"? Все карточки, относящиеся к ней, также будут удалены.",
                    PrimaryButtonText = "Удалить",
                    CloseButtonText = "Отмена",
                    XamlRoot = this.Content.XamlRoot,
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    disciplineGroups.RemoveAll(g => g.Discipline == discipline);
                    await SaveData();
                }
            }
        }

        private async void ExportDisciplineButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is string discipline)
            {
                var disciplineGroup = disciplineGroups.FirstOrDefault(d => d.Discipline.Equals(discipline));
                if (disciplineGroup != null)
                {
                    var savePicker = new Windows.Storage.Pickers.FileSavePicker();

                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                    WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

                    savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                    savePicker.FileTypeChoices.Add("JSON file", new List<string> { ".json" });
                    savePicker.SuggestedFileName = discipline;

                    var file = await savePicker.PickSaveFileAsync();
                    if (file != null)
                    {
                        string json = System.Text.Json.JsonSerializer.Serialize(disciplineGroup);
                        await Windows.Storage.FileIO.WriteTextAsync(file, json);
                    }
                }
            }
        }

        private async void ImportDisciplineButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                var savePicker = new Windows.Storage.Pickers.FileOpenPicker();

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeFilter.Add(".json");

                var file = await savePicker.PickSingleFileAsync();
                if (file != null)
                {
                    var loadedGroup = await LoadFromFile<DisciplineGroup>(file.Path);

                    if (loadedGroup == null)
                    {
                        var dialog = new ContentDialog
                        {
                            Title = "Ошибка",
                            Content = "Не удалось загрузить дисциплину из файла.",
                            CloseButtonText = "OK",
                            XamlRoot = this.Content.XamlRoot,
                        };
                        _ = dialog.ShowAsync();

                        return;
                    }

                    var disciplineGroup = disciplineGroups.FirstOrDefault(d => d.Discipline.Equals(loadedGroup.Discipline));

                    if (disciplineGroup != null)
                    {
                        disciplineGroup.Cards.AddRange(loadedGroup.Cards);
                    }
                    else
                    {
                        disciplineGroups.Add(loadedGroup);
                        UpdateUI();
                        await SaveToFile(disciplineGroups, flashCardsFile);
                    }
                }

            }
        }

        // Редактирование карточки. Передаем также дисциплину, чтобы знать, в какой группе она находится.
        private async void EditFlashCard(FlashCard card, string discipline)
        {
            var questionBox = new TextBox
            {
                Text = card.Question,
                Height = 100,
                Width = 500,
                TextWrapping = TextWrapping.Wrap,
            };

            var answerBox = new TextBox
            {
                AcceptsReturn = true,
                Text = card.Answer,
                Height = 300,
                IsSpellCheckEnabled = true,
                Width = 500,
                TextWrapping = TextWrapping.Wrap,
            };

            ScrollViewer.SetHorizontalScrollBarVisibility(answerBox, ScrollBarVisibility.Auto);

            var panel = new StackPanel { Spacing = 10 };
            panel.Children.Add(new TextBlock { Text = "Вопрос:" });
            panel.Children.Add(questionBox);
            panel.Children.Add(new TextBlock { Text = "Ответ:" });
            panel.Children.Add(answerBox);

            var dialog = new ContentDialog
            {
                Title = "Редактировать карточку",
                Content = panel,
                PrimaryButtonText = "Сохранить",
                CloseButtonText = "Отмена",
                XamlRoot = this.Content.XamlRoot,
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                card.Question = questionBox.Text.Trim();
                card.Answer = answerBox.Text.Trim();
                await SaveData();
            }
        }

        // Удаление карточки из группы по дисциплине
        private void DeleteFlashCard(FlashCard card, string discipline)
        {
            var group = disciplineGroups.FirstOrDefault(g => g.Discipline == discipline);
            if (group != null)
            {
                group.Cards.Remove(card);
            }
        }

        #endregion

        #region Просмотр/Редактирование

        private void FilterDisciplineComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = e.AddedItems.FirstOrDefault();
            if (selected != null)
            {
                FilterDisciplineComboBox__lastSelected = (selected as string);
                UpdateFlashCardsStackPanel();
            }
        }

        #endregion

        #region Test Logic

        // Инициализация тестового пула: при выборе дисциплины берём карточки из соответствующей группы,
        // иначе – объединяем все карточки
        private void InitializeTestPool()
        {
            if (TestDisciplineComboBox.SelectedItem != null &&
                TestDisciplineComboBox.SelectedItem.ToString() != "Все")
            {
                string? selected = TestDisciplineComboBox.SelectedItem.ToString();
                var group = disciplineGroups.FirstOrDefault(g => g.Discipline == selected);
                testCardPool = group != null ? group.Cards.ToList() : new List<FlashCard>();
            }
            else
            {
                testCardPool = disciplineGroups.SelectMany(g => g.Cards).ToList();
            }
        }

        // Переход к следующей карточке теста
        private void NextCardButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentFlashCard != null)
            {
                // Если карточка уже была показана, удаляем её из тестового пула
                testCardPool.Remove(currentFlashCard);
            }

            if (testCardPool == null || testCardPool.Count == 0)
            {

                if (NextCardButton.Content as string == "Начать заново")
                {
                    InitializeTestPool();
                }
                else
                {
                    TestCardTextBlock.Text = "Нет карточек для теста.";
                    FlipButton.Visibility = Visibility.Collapsed;
                    DecisionButtonsPanel.Visibility = Visibility.Collapsed;
                    NextCardButton.Content = "Начать заново";
                    return;
                }

                //}
            }

            Random random = new Random();
            int index = random.Next(testCardPool?.Count ?? 0);
            currentFlashCard = testCardPool![index];

            TestCardTextBlock.Text = currentFlashCard.Question;
            FlipButton.Visibility = Visibility.Visible;
            DecisionButtonsPanel.Visibility = Visibility.Collapsed;

            NextCardButton.Content = testCardPool.Count == 0 ? "Начать заново" : "Следующий";
        }

        // Переворот карточки: показ ответа
        private void FlipButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentFlashCard == null) return;
            TestCardTextBlock.Text = currentFlashCard.Answer;
            FlipButton.Visibility = Visibility.Collapsed;
            DecisionButtonsPanel.Visibility = Visibility.Visible;
        }

        // Кнопка "Убрать": удаляет карточку из тестового пула
        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentFlashCard != null)
                testCardPool.Remove(currentFlashCard);
            NextCardButton_Click(sender, e);
        }

        // Кнопка "Оставить": оставляет карточку в тестовом пуле
        private void KeepButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentFlashCard != null)
            {
                testCardPool.Add(currentFlashCard);
            }

            NextCardButton_Click(sender, e);
        }

        #endregion

        private void TestDisciplineComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InitializeTestPool();
            NextCardButton_Click(null!, null!);
        }
    }

    // Теперь FlashCard содержит только вопрос и ответ
    public class FlashCard
    {
        public string Question { get; set; } = null!;
        public string Answer { get; set; } = null!;
    }

    // DisciplineGroup хранит имя дисциплины и список карточек (без дублирования дисциплины в каждой карточке)
    public class DisciplineGroup
    {
        public string Discipline { get; set; } = null!;
        public List<FlashCard> Cards { get; set; } = new List<FlashCard>();
    }
}
