using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonMerger
{
    // Модель данных: FlashCard
    public class FlashCard
    {
        public string Question { get; set; } = null!;
        public string Answer { get; set; } = null!;
    }

    // Модель данных: DisciplineGroup
    public class DisciplineGroup
    {
        public string Discipline { get; set; } = null!;
        public List<FlashCard> Cards { get; set; } = null!;
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            // Если директория передана в качестве аргумента, используем её, иначе текущую директорию
            string directoryPath = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
            var jsonFiles = Directory.GetFiles(directoryPath, "*.json");

            if (!jsonFiles.Any())
            {
                Console.WriteLine("В указанной директории не найдено JSON файлов.");
                return;
            }

            // Список для объединённых данных
            var mergedGroups = new List<DisciplineGroup>();

            foreach (var file in jsonFiles)
            {
                try
                {
                    string json = await File.ReadAllTextAsync(file);
                    var groups = JsonSerializer.Deserialize<List<DisciplineGroup>>(json);
                    if (groups != null)
                    {
                        foreach (var group in groups)
                        {
                            // Если группа с такой дисциплиной уже существует, объединяем карточки
                            var existingGroup = mergedGroups.FirstOrDefault(g => g.Discipline == group.Discipline);
                            if (existingGroup != null)
                            {
                                existingGroup.Cards.AddRange(group.Cards);
                            }
                            else
                            {
                                mergedGroups.Add(group);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при обработке файла {file}: {ex.Message}");
                }
            }

            // Опционально: удаляем дублирующиеся карточки в каждой группе (сравнение по Question и Answer)
            foreach (var group in mergedGroups)
            {
                group.Cards = group.Cards
                    .GroupBy(c => new { c.Question, c.Answer })
                    .Select(g => g.First())
                    .ToList();
            }

            // Сериализация объединённых данных в отформатированный JSON
            string mergedJson = JsonSerializer.Serialize(mergedGroups, new JsonSerializerOptions { WriteIndented = true });
            string outputFile = Path.Combine(directoryPath, "merged.json");
            await File.WriteAllTextAsync(outputFile, mergedJson);
            Console.WriteLine($"Объединённый JSON сохранён в файл: {outputFile}");
        }
    }
}
