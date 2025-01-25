using Photon.Pun;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    [SerializeField] private Material _demonMaterial; // Material for the demon
    [SerializeField] private GameObject _endScreen; // UI for the end screen
    [SerializeField] private PlayerSpawner _spawnManager; // Reference to the SpawnManager in the scene

    private PhotonView _currentDemon;
    public static GameManager Instance { get; private set; } // Singleton instance

    private void Awake()
    {

        // Set up the Singleton instance
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        // In GameManager's Start() or Awake


        if (_spawnManager == null)
        {
            Debug.LogError("SpawnManager is not assigned in the GameManager!");
        }
    }

    [PunRPC]
    public void SetTagOnStart(int viewID)
    {
        _currentDemon = PhotonView.Find(viewID);
        _currentDemon.gameObject.GetComponent<PlayerTagBehaviour>()._isTagged = true;
        _currentDemon.gameObject.tag = "It";
        if (_currentDemon != null)
        {
            MeshRenderer meshRenderer = _currentDemon.GetComponentInChildren<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.material = _demonMaterial;
            }
            else
            {
                Debug.Log("material not found on " + viewID);
            }
        }
    }

    [PunRPC]
    public void SetTAG(int currentDemonViewID, int newDemonViewID)
    {
        if (_currentDemon == null || _currentDemon.ViewID != currentDemonViewID) return;

        // Assign the new demon
        _currentDemon = PhotonView.Find(newDemonViewID);
        if (_currentDemon == null) return;

        // Set new demon's material
        MeshRenderer meshRenderer = _currentDemon.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.material = _demonMaterial;
        }

        // Respawn all players using SpawnManager
        _spawnManager.RespawnAllPlayers(newDemonViewID);
    }

    [PunRPC]
    public void EndGame()
    {
        if (_endScreen != null)
        {
            _endScreen.SetActive(true); // Show the end screen
        }

        Time.timeScale = 0f; // Pause the game
    }
}
