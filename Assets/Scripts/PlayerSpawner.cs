using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private Transform _demonSpawnPoint; // Specific spawn for the demon
    [SerializeField] private List<Transform> _spawnPoints; // List of normal spawn points

    private PhotonView _gameMangaerView;
    private bool _playerSpawned = false; // Ensures each player spawns only once

    private void Start()
    {
        _gameMangaerView = GameManager.Instance.GetComponent<PhotonView>();
        if (PhotonNetwork.InRoom && !_playerSpawned)
        {
            SpawnPlayer();
        }
    }

    public override void OnJoinedRoom()
    {
        // Ensure late joiners also spawn
        if (!_playerSpawned)
        {
            SpawnPlayer();
        }
    }

    public void SpawnPlayer()
    {
        // Ensure the player spawns only once
        _playerSpawned = true;

        Vector3 spawnPosition;

        if (PhotonNetwork.IsMasterClient)
        {
            // MasterClient spawns as the demon at the demon spawn point
            spawnPosition = _demonSpawnPoint.position;
            GameObject demonPlayer = PhotonNetwork.Instantiate(_playerPrefab.name, spawnPosition, Quaternion.identity);

            // Initialize demon settings via PlayerTagBehaviour
            PlayerTagBehaviour demonTagBehaviour = demonPlayer.GetComponent<PlayerTagBehaviour>();
           // demonTagBehaviour.GetComponent<PhotonView>().RPC(nameof(PlayerTagBehaviour.TakeDemonShip), RpcTarget.AllBuffered);
        }
        else
        {
            // Normal players spawn at random available spawn points
            spawnPosition = GetRandomSpawnPoint();
            GameObject normalPlayer = PhotonNetwork.Instantiate(_playerPrefab.name, spawnPosition, Quaternion.identity);

            // Ensure normal players are not tagged
            PlayerTagBehaviour normalTagBehaviour = normalPlayer.GetComponent<PlayerTagBehaviour>();
            normalTagBehaviour._isTagged = false;
        }
    }


    private Vector3 GetRandomSpawnPoint()
    {
        // Get a random spawn point from the list
        int randomIndex = Random.Range(0, _spawnPoints.Count);
        return _spawnPoints[randomIndex].position;
    }

    public void RespawnAllPlayers(int newDemonViewID)
    {
        Photon.Realtime.Player[] players = PhotonNetwork.PlayerList;

        foreach (Photon.Realtime.Player player in players)
        {
            // Find the player's PhotonView
            PhotonView playerView = PhotonView.Find(player.ActorNumber + 1); // Ensure correct view ID lookup
            if (playerView == null) continue;

            // Check if this player is the new demon
            if (playerView.ViewID == newDemonViewID)
            {
                // New demon goes to the demon spawn point
                playerView.transform.position = _demonSpawnPoint.position;
            }
            else
            {
                // Other players go to random spawn points
                playerView.transform.position = GetRandomSpawnPoint();
            }
        }
    }

}