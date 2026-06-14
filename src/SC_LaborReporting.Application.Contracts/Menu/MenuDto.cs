using System;
using System.Collections.Generic;
using System.Text;

namespace SC_LaborReporting.Menu
{
    public class MenuDto
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Icon { get; set; }
        public List<MenuDto> Children { get; set; }
    }
}
