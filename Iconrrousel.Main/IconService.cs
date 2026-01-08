using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iconrrousel.Main
{
    public interface IIconService
    {
        event Action OnDeleteAllIcons;
        void DeleteAllIcons();
    }

    public class IconService : IIconService
    {
        public event Action OnDeleteAllIcons;

        public void DeleteAllIcons()
        {
            OnDeleteAllIcons?.Invoke();
        }
    }
}
