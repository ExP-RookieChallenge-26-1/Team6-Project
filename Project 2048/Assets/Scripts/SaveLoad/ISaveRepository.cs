namespace Project2048.Save
{
    public interface ISaveRepository
    {
        bool Exists();
        void Save(GameSaveData data);
        GameSaveData Load();
        void Delete();
    }
}
