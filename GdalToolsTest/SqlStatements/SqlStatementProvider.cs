using System.Collections.Generic;

namespace GdalCoreTest.SqlStatements;

internal class SqlStatementProvider
{

    public static List<SqlStatementForWRZ> BuildList()
    {
        var statements = new List<SqlStatementForWRZ>();

        string layerName = "wildruhezone";

        string orderByFieldname = "Name";

        // ORDER BY
        statements.Add(new SqlStatementForWRZ(layerName, "[fieldname] = 'S10' , [fieldname] LIKE 'E%'",
            $"SELECT * FROM {layerName} ORDER BY {orderByFieldname}"));

        // LENGTH
        statements.Add(new SqlStatementForWRZ(layerName, "[fieldname] = 'S10' , [fieldname] LIKE 'E%'",
            $"SELECT * FROM {layerName} WHERE LENGTH({orderByFieldname}) < 100"));

        // CHAR_LENGTH(string_exp)
        // Returns the length in characters of the string expression.
        // 
        // LOWER(string_exp)
        // Returns a string equal to that in string_exp, with all uppercase characters converted to lowercase.
        // 
        // POSITION(character_exp IN character_exp
        // Returns the position of the first character expression in the second character expression. The result is an exact numeric with an implementation-defined precision and a scale of zero.
        // 
        // SUBSTRING(string_exp FROM start FOR length)
        // Returns a character string that is derived from string_exp, beginning at the character position specified by start for length characters.
        // 
        // TRIM(BOTH | LEADING | TRAILING trim_character FROM string_exp)
        // Returns the string_exp with the trim_character removed from the leading, trailing, or both ends of the string.
        // 
        // UPPER(string_exp)
        // Returns a string equal to that in string_exp, with all lowercase characters converted to uppercase.








        // Falsche Zuordnung empfohlen vs. rechtsverbindlich   LIKE 'A%'
        statements.Add(new SqlStatementForWRZ(layerName, "[fieldname] = 'S10' , [fieldname] LIKE 'E%'",
            $"SELECT * FROM {layerName} WHERE (Schutzstatus = 'S10' AND Bestimmungen LIKE 'E%') OR (Schutzstatus = 'S20' AND Bestimmungen LIKE 'R%')"));

        // multipart key failure
        statements.Add(new SqlStatementForWRZ(layerName, "GROUP BY HAVING COUNT(*)",
            $"SELECT ObjNummer, TeilObjNummer, Kanton, COUNT(*) FROM {layerName} GROUP BY ObjNummer, TeilObjNummer, Kanton HAVING COUNT(*) > 1"));

        // Übrige Bestimungen ohne Erläuterung
        statements.Add(new SqlStatementForWRZ(layerName, "[fieldname] IS NULL , LENGTH([fieldname]) < 2",
            $"SELECT * FROM {layerName} WHERE (Bestimmungen = 'E900' OR Bestimmungen = 'R900') AND (Bestimmungen_Kt IS NULL OR LENGTH(Bestimmungen_Kt) < 2)"));

        // Kein Punkt mit Leerzeichen am Ende von Bestimmungen_Kt
        statements.Add(new SqlStatementForWRZ(layerName, "[fieldname] = 'S10' , SUBSTRING([fieldname], LENGTH([fieldname])-1,2) <> '. '",
            $"SELECT * FROM {layerName} WHERE (Bestimmungen = 'E900' OR Bestimmungen = 'R900') AND SUBSTRING(Bestimmungen_Kt, LENGTH(Bestimmungen_Kt)-1,2) <> '. '"));

        //  Leeres Bestimmungen_Kt
        statements.Add(new SqlStatementForWRZ(layerName, "[fieldname] NOT IN ('E900','R900'), [fieldname] IS NOT NULL",
            $"SELECT * FROM {layerName} WHERE Bestimmungen NOT IN ('E900','R900') AND Bestimmungen_Kt IS NOT NULL"));




        return statements;
    }

}
