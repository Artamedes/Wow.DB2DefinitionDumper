﻿using System.Data.Common;
using System.Reflection.Metadata;
using System.Text;

namespace Wow.DB2DefinitionDumper.DBD;

public class DbdInfo
{
    public string FileName { get; set; }
    public List<DbdColumn> Columns { get; } = new();
    public string LayoutHash { get; set; }
    public int FileDataId { get; set; } = 0;

    /// <summary>
    /// Parses the <see cref="DbdColumn"/> instance to a C like structure string.
    /// </summary>
    /// <returns></returns>
    public string GetColumns()
    {
        var padding = GetMaxPadding();
        
        var builder = new StringBuilder();
        foreach (var column in Columns)
        {
            if (string.IsNullOrEmpty(column.Comment))
                builder.Append($"{column.Type.PadRight(padding.Type)} {(column.Name + ";").PadRight(padding.Name)}");
            else
                builder.Append($"{column.Type.PadRight(padding.Type)} {(column.Name + ";").PadRight(padding.Type + padding.Name)}").Append($"// {column.Comment}");
            builder.AppendLine();
        }
        return builder.ToString();
    }

    public string DumpMeta()
    {
        int indexField = -1;
        int parentIndexField = -1;
        var fieldCount = Columns.Count;
        var fileFieldCount = Columns.Count;

        const string padding = "    ";

        var columnsBuild = new StringBuilder();

        for (int i = 0; i < Columns.Count; ++i)
        {
            var column = Columns[i];

            if (i == 0 && column.Field.isID && column.Field.isNonInline)
            {
                fieldCount--;
                fileFieldCount--;
                continue;
            }

            if (column.Field.isRelation)
            {
                parentIndexField = i + (indexField == -1 ? -1 : 0);
                fileFieldCount--;
            }

            //if (column.Field.isID)
            //    dbdColumn.Comment += "Relation ";
            //if (column.Field.isNonInline)
            //    dbdColumn.Comment += "Non-inline ";

            columnsBuild.Append(padding);
            columnsBuild.Append(padding);
            columnsBuild.Append(padding);
            columnsBuild.Append(ConvertColumnToMetaField(column));
            columnsBuild.AppendLine();
        }

        var builder = new StringBuilder();
        builder.Append($"struct {FileName}Meta");
        builder.AppendLine();
        builder.Append("{");
        builder.AppendLine();
        builder.Append(padding);
        builder.Append("static DB2Meta const* Instance()");
        builder.AppendLine();
        builder.Append(padding);
        builder.Append("{");
        builder.AppendLine();
        builder.Append(padding);
        builder.Append(padding);
        builder.Append($"static constexpr DB2MetaField fields[{fieldCount}] =");
        builder.AppendLine();
        builder.Append(padding);
        builder.Append(padding);
        builder.Append("{");
        builder.AppendLine();
        builder.Append(columnsBuild);
        builder.Append(padding);
        builder.Append(padding);
        builder.Append("}");
        builder.AppendLine();
        builder.Append(padding);
        builder.Append(padding);
        builder.Append($"static constexpr DB2Meta instance({FileDataId}, {indexField}, {fieldCount}, {fileFieldCount}, 0x{LayoutHash}, fields, {parentIndexField});");
        builder.AppendLine();
        builder.Append(padding);
        builder.Append(padding);
        builder.Append($"return &instance;");
        builder.AppendLine();
        builder.Append(padding);
        builder.Append("}");
        builder.AppendLine();
        builder.Append("};");

        return builder.ToString();
    }

    private string ConvertColumnToMetaField(DbdColumn column)
    {
        StringBuilder builder = new StringBuilder();

        builder.Append("{ ");

        var field = column.Field;
        var isArray = field.arrLength != 0;

        switch (column.Column.type)
        {
            case "int":
            {
                var typeString = field.size switch
                {
                    8 => "FT_BYTE",
                    16 => "FT_SHORT",
                    32 => "FT_INT",
                    64 => "FT_LONG",
                    _ => new("Invalid field size")
                };
                builder.Append(typeString);
                break;
            }
            case "string":
            {
                builder.Append("FT_STRING_NOT_LOCALIZED");
                break;
            }
            case "locstring":
            {
                builder.Append("FT_STRING");
                break;

            }
            case "float":
            {
                builder.Append("FT_FLOAT");
                break;
            }
            default:
                throw new ArgumentException("Unable to construct C++ type from " + column.Type);
        }

        builder.Append(", ");
        builder.Append(isArray ? field.arrLength : 1);
        builder.Append(", ");
        if (field.isSigned)
            builder.Append("true");
        else
            builder.Append("false");
        builder.Append(" },");
        return builder.ToString();
    }

    private (int Name, int Type) GetMaxPadding()
    {
        var typePadding = 6;
        var namePadding = 20;
        foreach (var column in Columns)
        {
            if (column.Type.Length > typePadding)
                typePadding = column.Type.Length + 1;
            if (column.Name.Length > namePadding)
                namePadding = column.Name.Length + 1;
        }

        return (namePadding, typePadding);
    }
}