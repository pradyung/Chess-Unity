using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIScript : MonoBehaviour
{
    public MainScript mainScript;
    public RelayScript relayScript;

    public GameObject startingUIHolder;
    public GameObject titleScreenUI;
    public GameObject startGamePanel;
    public GameObject multiplayerPanel;
    public GameObject joinGamePanel;
    public GameObject waitingForJoinPanel;
    public GameObject gameOverPanelHost;
    public GameObject gameOverPanelClient;
    public GameObject gameOverPanel;

    public Text joinCodeOutput;
    public InputField joinCodeInput;
    public Text joiningStatus;
    public Text gameOverTextHost;
    public Text gameOverTextClient;

    public char gameMode;

    public void quitGame() => Application.Quit();

    public void startGame(bool sideBySide)
    {
        gameMode = sideBySide ? 'S' : 'M';
        if (sideBySide)
        {
            startingUIHolder.SetActive(false);
            mainScript.startGame(true, true);
        }
        else
        {
            relayScript.initializeRelay();
            multiplayerPanel.SetActive(true);
        }
    }

    public void playAgain()
    {
        mainScript.destroyBoard();

        switch (gameMode)
        {
            case 'M':
                relayScript.startGame(false, !mainScript.playerIsWhite);
                break;
            case 'S':
                mainScript.startGame(true, true);
                break;
        }
    }

    public void resetStartingUI()
    {
        startingUIHolder.SetActive(true);
        titleScreenUI.SetActive(true);

        foreach (GameObject panel in new GameObject[] { startGamePanel, multiplayerPanel, joinGamePanel, waitingForJoinPanel })
            panel.SetActive(false);
    }

    public void showGameOverPanel(string gameOverMessage)
    {
        gameOverPanel.SetActive(true);
        if (gameMode == 'S' || mainScript.hostMode)
        {
            gameOverPanelHost.SetActive(true);
            gameOverTextHost.text = gameOverMessage;
        }
        else
        {
            gameOverPanelClient.SetActive(true);
            gameOverTextClient.text = gameOverMessage;
        }
    }
}
