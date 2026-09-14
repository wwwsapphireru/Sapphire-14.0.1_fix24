namespace AdvantShop.Module.OneSApi.Models.Admin
{
    public class SelectItemModel
    {
        public SelectItemModel() { }

        public SelectItemModel(string label, string value, bool selected = false)
        {
            this.label = label;
            this.value = value;
            this.selected = selected;
        }

        public SelectItemModel(string label, int value, bool selected = false)
        {
            this.label = label;
            this.value = value.ToString();
            this.selected = selected;
        }

        public string label { get; set; }
        public string value { get; set; }
        public bool selected { get; set; }
    }

    public class SelectItemModel<T>
    {
        public SelectItemModel() { }

        public SelectItemModel(string label, T value, bool selected = false)
        {
            this.label = label;
            this.value = value;
            this.selected = selected;
        }

        public string label { get; set; }
        public T value { get; set; }
        public bool selected { get; set; }
    }
}
