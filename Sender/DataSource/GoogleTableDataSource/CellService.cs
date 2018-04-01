using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sender.DataSource.GoogleTabledataSource
{
    public interface ICellService {
        string GetFullRange(string[] cells);
        string GetMinCell(string[] cells);
        string GetMaxCell(string[] cells);
        string GetNextCell(string cell);
        int GetCellIndex(string range, string cell);
    }
    public class CellService : ICellService
    {
        public int GetCellIndex(string range, string cell)
        {
            var rangeAsArray = range.Split(":");
            var currentCell = rangeAsArray[0];
            var index = 0;
            while (true) {
                if (currentCell == cell)
                    break;
                if (currentCell == rangeAsArray[1]) {
                    index = -1;
                    break;
                } 
                currentCell = GetNextCell(currentCell);
                index++;
            }
            if (index == -1)
                throw new ArgumentOutOfRangeException("Cell not in range!");

            return index;
        }

        public string GetFullRange(string[] cells)
        {
            var orderedCells = cells
                .OrderBy(x => x.Length)
                .OrderBy(x => x.Length);
            return $"{orderedCells.First()}:{orderedCells.Last()}";
        }

        public string GetMaxCell(string[] cells)
        {
            return cells
                .OrderBy(x => x.Length)
                .OrderBy(x => x.Length).Last();
        }

        public string GetMinCell(string[] cells)
        {
            return cells
                .OrderBy(x => x)
                .OrderBy(x => x.Length).First();
        }

        public string GetNextCell(string cell)
        {
            char last = cell.ToUpper()[cell.Length - 1];
            if (last != 'Z') {
                last++;
                cell = cell.Substring(0, cell.Length - 1) + last;
            }
            else
            {
                last = 'A';
                cell = cell.Substring(0, cell.Length - 1) + last + last;
            }

            return cell;
        }
    }
}
