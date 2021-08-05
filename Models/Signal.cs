using LDF_File_Parser.Extension;
using LDF_File_Parser.Logger;
using System;
using System.Collections;
using System.Data;

namespace LDF_FILEPARSER
{

    public class Signal
    {
        public ICollection BitValues { get; set; }
        public EncodingNode Encoding { get; set; }
        public string InitalValue { get; private set; }
        public string Name { get; private set; }
        public string[] RawSignalValues { get; }
        public int Size { get; private set; }
        public int StartAddress { get; set; }

        // TODO Experiment
        public DataTable BitMap { get; set; }

        public Signal(string name, string size, string initalValue, string[] rawSignalValues)
        {

            #region Parameter Validation
            if (string.IsNullOrEmpty(name))
                throw new System.ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));

            if (string.IsNullOrEmpty(size))
                throw new System.ArgumentException($"'{nameof(size)}' cannot be null or empty.", nameof(size));

            if (string.IsNullOrEmpty(initalValue))
                throw new System.ArgumentException($"'{nameof(initalValue)}' cannot be null or empty.", nameof(initalValue));

            if (rawSignalValues is null)
                throw new ArgumentNullException(nameof(rawSignalValues));


            #endregion

            Name = name;
            Size = int.Parse(size);
            InitalValue = initalValue.ConvertToHex();
            RawSignalValues = rawSignalValues;
            BitValues = new BitArray(Size);

            BitMap = new DataTable("BitMap");

            try
            {
                var row = BitMap.NewRow();

                for (int i = Size - 1; i >= 0; i--)
                {
                    string columnName = $"{i}";
                    var column = new DataColumn(columnName) { ReadOnly = false };

                    column.DataType = Type.GetType("System.Boolean");
                    //column.DataType = Type.GetType("System.Int64");

                    BitMap.Columns.Add(column);


                    row[column] = false;
                    //row[column] = 0;
                }

                BitMap.Rows.Add(row);
                BitMap.DefaultView.AllowNew = false;
                BitMap.ColumnChanged += BitMap_ColumnChanged;

            }
            catch (Exception exc)
            {
                Logger.LogError(exc);
            }
        }

        private void BitMap_ColumnChanged(object sender, DataColumnChangeEventArgs e)
        {

            //object newValue = (int)e.Row[e.Column.ColumnName];
            //object oldValue = (int)e.Row[e.Column.ColumnName, DataRowVersion.Current];
            //var test = $"Column_Changed Event: name={newValue}; Column={e.Column.ColumnName}; original name={oldValue}";

        }

        public override string ToString() => $"Name: {Name}, Size: {Size}, StartAddress: {StartAddress}, Initial Value: {InitalValue}";

        public void UpdateEncoding(EncodingNode encoding)
        {
            if (encoding is null)
                throw new ArgumentNullException(nameof(encoding));

            Encoding = encoding;
        }
    }
}