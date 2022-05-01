﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilLibs
{
    public static class UtilMethods
    {
        public static bool IsCellInSpaceAndVacuum(int _cell)
        {
            return (Grid.IsCellOpenToSpace(_cell) || IsCellInRocket(_cell)) && Grid.Mass[_cell] == 0;
        }
        private static bool IsCellInRocket(int _cell)
        {
            WorldContainer w = Grid.IsValidCell(_cell) && (int)Grid.WorldIdx[_cell] != (int)ClusterManager.INVALID_WORLD_IDX ? ClusterManager.Instance.GetWorld((int)Grid.WorldIdx[_cell]) : (WorldContainer)null;
            return w.IsModuleInterior;
        }
    }
}
