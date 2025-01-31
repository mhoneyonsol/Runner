using Data;
using Helpers;
using UnityEngine;

namespace Components
{
    public class Level : MonoBehaviour
    {
        private Boss _boss;
        private Finish _finish;
        private bool _levelEnded = false;

        private Crowd _playerCrowd;
        private Crowd[] _enemyCrowds;
        private Gates[] _gates;

        private int _enemiesDefeated;
        private int _playersFinished;
        private Finish _finishLine;

        public int LvlNum { get; private set; }
        public Boss Boss => _boss;
        public Crowd PlayerCrowd => _playerCrowd;
        public Crowd[] EnemyCrowds => _enemyCrowds;

        private int _gateInd;
        private int _lvlReward;

        private GameData _gameData; // Added to store GameData reference

        public void Initialize(int lvlNum, GameData gameData)
{
    _gameData = gameData;
    _boss = GetComponentInChildren<Boss>();
    _finish = GetComponentInChildren<Finish>();
    _enemyCrowds = GetComponentsInChildren<Crowd>();
    _gates = GetComponentsInChildren<Gates>();
    _playerCrowd = new GameObject("PlayerCrowd").AddComponent<Crowd>();
    _playerCrowd.transform.SetParent(transform);
    _levelEnded = false;

    LvlNum = lvlNum;
    var animations = new[]
    {
        gameData.ManAnimation_Idle,
        gameData.ManAnimation_Run,
        gameData.ManAnimation_Fight,
        gameData.ManAnimation_Win,
    };

    var manHitDelay = gameData.ManHitMoment * gameData.ManAnimation_Fight.length;

    if (_boss) Boss.Initialize();

    PlayerCrowd.Initialize(gameData.PlayerManPrefab, manHitDelay, animations, gameData.MenCountLabelPrefab);

    // Initialize ghost companion if it exists
    GameObject existingGhost = GameObject.Find("Ghost_animation(Clone)");
    if (existingGhost != null)
    {
        Companion companionBehavior = existingGhost.GetComponent<Companion>();
        if (companionBehavior != null)
        {
            companionBehavior.Initialize(PlayerCrowd);
        }
    }

    // Initialize enemy crowds and calculate initial reward
    foreach (var crowd in EnemyCrowds)
    {
        crowd.Initialize(gameData.EnemyManPrefab, manHitDelay, animations);
        _enemiesDefeated += crowd.MenCount;
    }

    // Debug log the initial enemy count
    Debug.Log($"Initial enemies count: {_enemiesDefeated}");

    foreach (var gate in _gates)
    {
        gate.Initialize(gameData.PositiveColor, gameData.NegativeColor);
    }

    _lvlReward = _enemiesDefeated * gameData.KillReward;
    Debug.Log($"Initial reward set to: {_lvlReward}");

    GameEvents.OnBossFightEnd += EndLevel;
    GameEvents.OnGameUpdate += OnUpdate;
}


        public void DeInitialize()
        {
            GameEvents.OnGameUpdate -= OnUpdate;
            GameEvents.OnBossFightEnd -= EndLevel;

            _lvlReward = 0;
            _enemiesDefeated = 0;
            _playersFinished = 0;
            
            PlayerCrowd.DeInitialize();
            foreach (var crowd in EnemyCrowds)
            {
                crowd.DeInitialize();
            }
        }

        private void CheckGatesAchieved()
        {
            if (_gateInd >= _gates.Length)
                return;

            var manPos = _playerCrowd.ForwardManWorldPos;
            var areGatesAchieved = manPos.z >= _gates[_gateInd].transform.position.z;
            if (areGatesAchieved)
            {
                var value = _gates[_gateInd++].Catalyse(manPos);
                _playerCrowd.ChangeMenCount(value);
            }
        }

        private void CheckFinishAchieved()
{
    if (!_finish)
        return;

    var isFinishAchieved = _playerCrowd.WorldPos.z > _finish.transform.position.z;
    if (isFinishAchieved && !_levelEnded)
    {
        _playerCrowd.SetAnimation(AnimationType.Win);
                
        // Count ALL men in the player crowd as finished
        _playersFinished = _playerCrowd.MenCount;
        
        // Ajout du code pour le mode course ici
        if (RaceSession.Instance != null)
        {
            RaceSession.Instance.FinishRace(true);
        }

        // Update total reward to include both enemies and ALL players
        _lvlReward = (_enemiesDefeated + _playersFinished) * _gameData.KillReward;
        
        GameEvents.FinishAchieved();
        EndLevel(true);
    }
}
private void EndLevel(bool isPlayerWin)
{
    if (isPlayerWin && !_levelEnded)  // Add flag to prevent multiple calls
    {
        _levelEnded = true;  // Add this flag at the top of the class: private bool _levelEnded = false;
        
        PlayerPrefs.SetInt($"Level_{LvlNum}_Done", 1);
        PlayerPrefs.Save();

        // No need to recalculate here - use the value we calculated in CheckFinishAchieved
        Debug.Log($"Sending final reward: {_lvlReward}");
        GameEvents.LevelEnd(_lvlReward);
    }

    DeInitialize();
}

        private void OnUpdate(float deltaTime)
        {
            CheckFinishAchieved();
            CheckGatesAchieved();
            
            // Update reward during gameplay if players are finishing
            if (_finishLine != null)
            {
                int currentFinished = _finishLine.GetFinishCount();
                if (currentFinished != _playersFinished)
                {
                    _playersFinished = currentFinished;
                    _lvlReward = (_enemiesDefeated + _playersFinished) * _gameData.KillReward;
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
                return;

            var positions = ValuesCounter.HexagonPositions;
            var unitSize = 0.5f;
            var unitScale = Vector3.one * unitSize;
            var heightOffset = Vector3.up * unitSize * 0.5f;
            
            Gizmos.color = Color.blue;
            Gizmos.DrawCube(Vector3.zero + heightOffset, unitScale);

            Gizmos.color = Color.red;
            foreach (var enemyCrowd in GetComponentsInChildren<Crowd>())
            {
                for (var i = 0; i < enemyCrowd.MenStartCount; i++)
                {
                    Gizmos.DrawCube(enemyCrowd.transform.position + positions[i] + heightOffset, unitScale);
                }
            }
        }
    }
}