using System.Collections.Generic;
using Project2048.Core;
using Project2048.Rewards;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Save
{
    public class SaveLoadManager : MonoBehaviour
    {
        private ISaveRepository saveRepository;
        private GameContext gameContext;
        private GameSaveData loadedData;

        public bool HasSave => saveRepository != null && saveRepository.Exists();
        public bool HasLoadedRunProgress => loadedData != null;

        public void Initialize(GameContext context, ISaveRepository repository = null)
        {
            gameContext = context;
            saveRepository = repository ?? new JsonFileSaveRepository(SavePaths.DefaultSaveFilePath);
        }

        public void SaveRun(RunProgress runProgress)
        {
            if (gameContext == null)
            {
                Debug.LogError("GameContext is not initialized.");
                return;
            }

            saveRepository?.Save(GameSaveData.From(gameContext, runProgress));
        }

        public void SaveInitialRun()
        {
            SaveRun(new RunProgress());
        }

        public bool TryLoadGameContext()
        {
            if (gameContext == null)
            {
                Debug.LogError("GameContext is not initialized.");
                return false;
            }

            if (saveRepository == null || !saveRepository.Exists())
            {
                return false;
            }

            loadedData = saveRepository.Load();
            if (loadedData == null)
            {
                return false;
            }

            loadedData.ApplyTo(gameContext);
            return true;
        }

        public bool TryApplyLoadedRunProgress(RunProgress runProgress, IEnumerable<SkillSO> knownSkills = null)
        {
            if (loadedData == null || runProgress == null)
            {
                return false;
            }

            loadedData.ApplyTo(runProgress, knownSkills);
            loadedData = null;
            return true;
        }

        public void DeleteSave()
        {
            loadedData = null;
            saveRepository?.Delete();
        }
    }
}
