namespace Gameplay.UI.Popups
{
    public interface IModalPopup
    {
        public bool IsDisplaying { get; }

        public event System.Action<IModalPopup> OnClose;


        public void Open();
        public void Close();
    }
}