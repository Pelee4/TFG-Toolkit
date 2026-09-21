using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;


namespace TFGToolkit
{

// -------------------------------------------------------
// Tipos de campo soportados
// -------------------------------------------------------
public enum CustomFieldType
 {
    Text, 
    Number,
    CheckBox,
    Dropdown
}


// -------------------------------------------------------
// Definición de un campo
// -------------------------------------------------------
[Serializable]
public class CustomFieldDef
{
    public string               Id = "";
    public string               Label = "Nuevo campo";
    public CustomFieldType      Type = CustomFieldType.Text;
    public List<string>         DropdownOptions = new List<string>(); // solo para Dropdown
    public string               DefaultValue = "";
    public bool                 ShowInCard = true;
}

// -------------------------------------------------------
// Esquema separado por tipo de asset
// -------------------------------------------------------
[Serializable]
public class CustomFieldSchema
{
    public List<CustomFieldDef> MechanicFields  = new List<CustomFieldDef>();
    public List<CustomFieldDef> CharacterFields = new List<CustomFieldDef>();
}

// -------------------------------------------------------
// Un valor concreto para un campo en un asset
// -------------------------------------------------------
[Serializable]
public class CustomFieldValue
{
    public string FieldId = "";
    public string Value = ""; //Se convierte al mostrarlo segun su tipo
}

// -------------------------------------------------------
// Todos los valores de un asset (identificado por GUID)
// -------------------------------------------------------
[Serializable]
public class AssetCustomData
{
    public string AssetGuid = "";
    public List<CustomFieldValue> Values = new List<CustomFieldValue>();
}

// -------------------------------------------------------
// Todos los valores del proyecto
// ------------------------------------------------------
[Serializable]
public class CustomFieldValuesStore
{
    public List<AssetCustomData> Assets = new List<AssetCustomData>();
}

// -------------------------------------------------------
// Servicio estático — carga, guarda y accede a los datos
// -------------------------------------------------------
public static class CustomFieldService
{
    private const string SCHEMA_PATH = "Assets/TFGToolkit/Data/CustomFields/CustomFieldSchema.json";
    private const string VALUES_PATH = "Assets/TFGToolkit/Data/CustomFields/CustomFieldValues.json";

    private static CustomFieldSchema      _schema;
    private static CustomFieldValuesStore _values;

    public static CustomFieldSchema Schema
    {
        get { if ( _schema == null ) LoadSchema(); return _schema; }
    }

    public static CustomFieldValuesStore Values
    {
        get { if (_values == null) LoadValues(); return _values; }
    }


    // --Lectura--

    public static string GetValue(string assetGuid, string fieldId)
    {
        var assetData = Values.Assets.Find(a => a.AssetGuid == assetGuid);
        if (assetData == null) return "";
        var fieldVal = assetData.Values.Find(v => v.FieldId == fieldId);
        return fieldVal?.Value ?? "";
    }

    // --Escritura--

    public static void SetValue (string assetGuid, string fieldId, string value)
    {
        var assetData = Values.Assets.Find(a => a.AssetGuid == assetGuid);
        if (assetData == null)
        {
            assetData = new AssetCustomData { AssetGuid = assetGuid };
            Values.Assets.Add(assetData);
        }

        var fieldVal = assetData.Values.Find(v => v.FieldId == fieldId);
        if (fieldVal == null)
        {
            fieldVal = new CustomFieldValue { FieldId = fieldId };
            assetData.Values.Add(fieldVal);
        }

        fieldVal.Value = value;
        SaveValues();
    }

    // --Persistencia--

    public static void LoadSchema()
    {
        if (!File.Exists(SCHEMA_PATH))
        {
            _schema = new CustomFieldSchema();
            return;
        }
        try { _schema = JsonUtility.FromJson<CustomFieldSchema>(File.ReadAllText(SCHEMA_PATH)) ?? new CustomFieldSchema(); }
        catch { _schema = new CustomFieldSchema(); }
    }

    public static void SaveSchema()
    {
        EnsureFolder();
        File.WriteAllText(SCHEMA_PATH, JsonUtility.ToJson(_schema, prettyPrint: true));
        AssetDatabase.Refresh();
    }

    public static void LoadValues()
    {
        if (!File.Exists(VALUES_PATH))
        {
            _values = new CustomFieldValuesStore();
            return;
        }
        try { _values = JsonUtility.FromJson<CustomFieldValuesStore>(File.ReadAllText(VALUES_PATH)) ?? new CustomFieldValuesStore(); }
        catch { _values = new CustomFieldValuesStore(); }
    }

    public static void SaveValues()
    {
        EnsureFolder();
        File.WriteAllText(VALUES_PATH, JsonUtility.ToJson(_values, prettyPrint: true));
    }

    public static void Reload()
    {
        _schema = null;
        _values = null;
    }

    private static void EnsureFolder()
    {
        string folder = Path.GetDirectoryName(SCHEMA_PATH);
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
    }

    /// Devuelve el GUID de Unity de un asset
    public static string GetGuid(UnityEngine.Object asset)
    {
        string path = AssetDatabase.GetAssetPath(asset);
        return AssetDatabase.AssetPathToGUID(path);
    }

}


}
