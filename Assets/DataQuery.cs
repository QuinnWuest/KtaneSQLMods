﻿using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a query over a DataSet.
/// </summary>
public class DataQuery
{
    /// <summary>
    /// Represents the selection expressions the query will do on the data set.
    /// </summary>
    public readonly List<DataQuerySelection> selections = new List<DataQuerySelection>();

    /// <summary>
    /// Represents the filters applied on the data set, this is only one filter but the filter can be a boolean filter of other filters.
    /// </summary>
    /// <remarks>Remember that filters already implement the concept of parenthesis so you have to build the and/or combinations yourself properly.</remarks>
    public DataQueryFilter filter = null;

    /// <summary>
    /// Represents the group on the data set.
    /// </summary>
    public readonly DataQueryGroup groupby = new DataQueryGroup();

    /// <summary>
    /// Represents the limitations on the result set.
    /// </summary>
    public readonly DataQueryLimitation limits = new DataQueryLimitation();

    /// <summary>
    /// Applies the query to a dataset
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public DataSet Apply(DataSet source)
    {
        // Create a new data set from valid rows
        DataSet result = new DataSet();
        result.rows = source.rows.Where(row => filter == null || filter.Apply(row)).ToList();

        // Now group and aggregate the results or just select the end results
        if (groupby.column != DataRowColumnEnum.None)
        {
            // Ensure that all selections are either aggregated or are the column grouped on
            if (selections.Where(s =>
            {
                return s.aggregator != DataQueryAggregatorEnum.None || (int)s.column == (int)groupby.column;
            }).Count() != selections.Count())
            {
                throw new InvalidOperationException("Must aggregate or group on each selection when using a group");
            }

            // Create the groups
            var groupedResults = from row in result.rows
                                 group row by row.GetValueByColumn(groupby.column) into groupedValues
                                 select groupedValues;

            // For each group, apply the aggregators on a new DataRow and add it to the result (after clearing all rows from it)
            List<DataRow> newRows = new List<DataRow>();
            foreach (var grouping in groupedResults)
            {
                DataRow resultingRow = new DataRow();
                int resultColIndex = 0;
                foreach (DataQuerySelection selection in selections)
                {
                    switch (selection.aggregator)
                    {
                        case DataQueryAggregatorEnum.Avg:
                            resultingRow.SetValueByColumn((DataRowColumnEnum)resultColIndex, (int)Math.Round(grouping.Average(row => row.GetValueByColumn(selection.column)), 0));
                            break;
                        case DataQueryAggregatorEnum.Count:
                            resultingRow.SetValueByColumn((DataRowColumnEnum)resultColIndex, grouping.Count());
                            break;
                        case DataQueryAggregatorEnum.Max:
                            resultingRow.SetValueByColumn((DataRowColumnEnum)resultColIndex, grouping.Max(row => row.GetValueByColumn(selection.column)));
                            break;
                        case DataQueryAggregatorEnum.Min:
                            resultingRow.SetValueByColumn((DataRowColumnEnum)resultColIndex, grouping.Min(row => row.GetValueByColumn(selection.column)));
                            break;
                        case DataQueryAggregatorEnum.Sum:
                            resultingRow.SetValueByColumn((DataRowColumnEnum)resultColIndex, grouping.Sum(row => row.GetValueByColumn(selection.column)));
                            break;
                        case DataQueryAggregatorEnum.None:
                        default:
                            resultingRow.SetValueByColumn((DataRowColumnEnum)resultColIndex, grouping.Key);
                            break;
                    }
                    resultColIndex++;
                }
                newRows.Add(resultingRow);
            }
            result.rows = newRows;
        }
        else
        {
            // Ensure that all selections are non aggregated
            if (selections.Where(s =>
            {
                return s.aggregator == DataQueryAggregatorEnum.None;
            }).Count() != selections.Count())
            {
                throw new InvalidOperationException("Must not aggregate on any selection when not using a group");
            }

            // Take only the selected fields
            for (int iRow = 0; iRow < result.rows.Count(); iRow++)
            {
                DataRow resultingRow = new DataRow();
                int resultColIndex = 0;
                foreach (DataQuerySelection selection in selections)
                {
                    if (selection.column == DataRowColumnEnum.None)
                    {
                        continue;
                    }
                    resultingRow.SetValueByColumn((DataRowColumnEnum)resultColIndex, result.rows[iRow].GetValueByColumn(selection.column));
                    resultColIndex++;
                }
                result.rows[iRow] = resultingRow;
            }

        }

        // Apply limits and return the result
        result.rows = result.rows.Skip(limits.linesSkiped).Take(limits.linesTaken == 0 ? 999 : limits.linesTaken).ToList();
        return result;

    }

    public override string ToString()
    {
        string result = "";
        result += "SELECT " + (from s in selections where s.column != DataRowColumnEnum.None select s.ToString()).Join(", ");
        result += filter != null ? " WHERE " + filter.ToString() : "";
        result += groupby.column != DataRowColumnEnum.None ? " GROUP BY " + ColumnEnumText(groupby.column) : "";
        result += " LIMIT " + (limits.linesTaken == 0 ? 999 : limits.linesTaken) + (limits.linesSkiped > 0 ? ", " + limits.linesSkiped : "");
        return result;
    }

    /// <summary>
    /// Simply returns the next data row column value based on current.
    /// </summary>
    /// <param name="current">Current enum value</param>
    /// <returns>Next enum value</returns>
    private string ColumnEnumText(DataRowColumnEnum current)
    {
        switch (current)
        {
            case DataRowColumnEnum.ColumnA: return "A";
            case DataRowColumnEnum.ColumnB: return "B";
            case DataRowColumnEnum.ColumnC: return "C";
            case DataRowColumnEnum.ColumnD: return "D";
            case DataRowColumnEnum.ColumnE: return "E";
            case DataRowColumnEnum.ColumnF: return "F";
            case DataRowColumnEnum.ColumnG: return "G";
            case DataRowColumnEnum.None: return "-";
            default: return "-";
        }
    }

}