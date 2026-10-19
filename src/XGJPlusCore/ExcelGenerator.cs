using OfficeOpenXml;

namespace XGJPlus;

public static class ExcelGenerator
{
    public static void SetupLicense()
    {
        // EPPlus 需要设置许可证上下文以运行
#pragma warning disable CS0618 // Type or member is obsolete
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
#pragma warning restore CS0618 // Type or member is obsolete
    }

    // 通用数据结构
    public class ProcessedData
    {
        public string Type { get; set; } = "";
        public List<Dictionary<string, string>> Rows { get; set; } = new();
        public List<string> Columns { get; set; } = new();
        public string FileName { get; set; } = "";
    }

    public static async Task GenerateExcelAsync(ProcessedData data)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("数据");

        // 设置列
        for (int i = 0; i < data.Columns.Count; i++)
        {
            var col = worksheet.Column(i + 1);
            col.Width = 20;
            worksheet.Cells[1, i + 1].Value = data.Columns[i];
        }

        int rowIndex = 2;

        if (data.Type == "score")
        {
            foreach (var row in data.Rows)
            {
                WriteRow(worksheet, rowIndex++, data.Columns, row);
            }
        }
        else if (data.Type == "studentInfo")
        {
            // 添加说明行
            WriteHeaderRow(worksheet, rowIndex++, data.Columns, "说明");
            WriteHeaderRow(worksheet, rowIndex++, data.Columns,
                "请大家核对学籍信息是否有误 ① 名字是否有误  ②性别是否有误  ③ 身份证号码是否有误，如有误请及时与班主任联系，谢谢！");
            rowIndex++; // 空行
            foreach (var row in data.Rows)
            {
                WriteRow(worksheet, rowIndex++, data.Columns, row);
            }
        }
        else if (data.Type == "classMember")
        {
            WriteHeaderRow(worksheet, rowIndex++, data.Columns, "说明");
            WriteHeaderRow(worksheet, rowIndex++, data.Columns,
                "请大家核对学籍信息是否有误 ① 名字是否有误  ②性别是否有误  ③ 身份证号码是否有误，如有误请及时与班主任联系，谢谢！");
            rowIndex++; // 空行
            foreach (var row in data.Rows)
            {
                WriteRow(worksheet, rowIndex++, data.Columns, row);
            }
        }

        var fileNameWithTime = $"{data.FileName}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.xlsx";
        var outputPath = Path.Combine(Directory.GetCurrentDirectory(), fileNameWithTime);
        await package.SaveAsAsync(outputPath);
        Console.WriteLine($"\n\n数据处理完毕，已保存到{outputPath}");
    }

    private static void WriteRow(ExcelWorksheet ws, int rowIndex, List<string> columns, Dictionary<string, string> data)
    {
        for (int i = 0; i < columns.Count; i++)
        {
            ws.Cells[rowIndex, i + 1].Value = data.TryGetValue(columns[i], out var val) ? val : "";
        }
    }

    private static void WriteHeaderRow(ExcelWorksheet ws, int rowIndex, List<string> columns, string firstValue)
    {
        ws.Cells[rowIndex, 1].Value = firstValue;
        // 合并单元格用于长文本说明
        if (columns.Count > 1)
        {
            ws.Cells[rowIndex, 2, rowIndex, columns.Count].Value = "";
        }
    }
}
