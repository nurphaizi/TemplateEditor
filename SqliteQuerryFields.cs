using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Data.SQLite;
using System.Collections.Generic;
using System.Diagnostics; // Для вывода в окно "Output" в Visual Studio

namespace TemplateEdit;
public class SqliteQuerryFields
{


    /// <summary>
    /// Получает список имен колонок из SQL-запроса к SQLite.
    /// </summary>
    /// <param name="connectionString">Строка подключения (напр., "Data Source=C:\temp\my.db")</param>
    /// <param name="sqlQuery">SQL-запрос (напр., "SELECT CustomerId as ID, Name FROM Customers")</param>
    /// <returns>Список имен колонок.</returns>
    public List<(string Name, System.Type type)> GetColumnNamesFromQuery(string connectionString, string sqlQuery)
    {
        var columnNames = new List<(string Name, System.Type type)>();

        // Используем 'using' для автоматического закрытия соединения и ридера
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();

            using (var command = new SQLiteCommand(sqlQuery, connection))
            {
                // Выполняем запрос
                // Behavior.SchemaOnly говорит, что нам нужны только метаданные (колонки),
                // а не сами данные. Это быстрее, если вам НЕ нужно читать строки.
                // Если вам ДАЛЬШЕ нужно читать данные, используйте просто command.ExecuteReader()
                using (var reader = command.ExecuteReader(System.Data.CommandBehavior.SchemaOnly))
                {
                    // 'FieldCount' содержит количество колонок в результате запроса
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        // 'GetName(i)' получает имя колонки по ее индексу (от 0)
                        columnNames.Add((reader.GetName(i),reader.GetFieldType(i)));
                    }
                }
            }
        }

        // (Для отладки в WPF) Выводим результат в окно Output
        Debug.WriteLine("Найденные колонки:");
        foreach (var name in columnNames)
        {
            Debug.WriteLine($"- {name.Name} {name.type}");
        }

        return columnNames;
    }

}
