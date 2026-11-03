using NoOpArmy.WiseFeline.BlackBoards;
using UnityEngine;

/// <summary>
/// Handles the agent needs changes so every frame each need is increased by some amount which is a bit randomized to create variety in behaviors
/// </summary>
public class LightDemoAgent : MonoBehaviour
{
    private BlackBoard myBlackBoard;
    [SerializeField]
    private GameObject bedGameObject;
    [SerializeField]
    private GameObject wcGameObject;
    [SerializeField]
    private GameObject refrigeratorGameObject;
    [SerializeField]
    private GameObject tvGameObject;

    private void Awake()
    {
        myBlackBoard = GetComponent<BlackBoard>();
        myBlackBoard.SetFloat("Hunger", Random.Range(0f, 1f));
        myBlackBoard.SetFloat("Energy", Random.Range(0f, 1f));
        myBlackBoard.SetFloat("Bladder", Random.Range(0f, 1f));
        myBlackBoard.SetFloat("Entertainment", Random.Range(0f, 1f));



        myBlackBoard.SetGameObject("Bed", bedGameObject);
        myBlackBoard.SetGameObject("WC", wcGameObject);
        myBlackBoard.SetGameObject("Refrigerator", refrigeratorGameObject);
        myBlackBoard.SetGameObject("TV", tvGameObject);
    }

    float personalizedHunger;
    float personalizedEnergy;
    float personalizedBladder;
    float personalizedEntertainment;

    void Start()
    {
        personalizedHunger = Random.Range(0.01f, 0.02f);
        personalizedEnergy = Random.Range(0.01f, 0.02f);
        personalizedBladder = Random.Range(0.01f, 0.02f);
        personalizedEntertainment = Random.Range(0.01f, 0.02f);

    }

    void Update()
    {
        float hunger = myBlackBoard.GetFloat("Hunger");
        if (hunger > 0)
            myBlackBoard.SetFloat("Hunger", hunger - personalizedHunger * Time.deltaTime);


        float energy = myBlackBoard.GetFloat("Energy");
        if (energy > 0)
            myBlackBoard.SetFloat("Energy", energy - personalizedEnergy * Time.deltaTime);

        float bladder = myBlackBoard.GetFloat("Bladder");
        if (bladder > 0)
            myBlackBoard.SetFloat("Bladder", bladder - personalizedBladder * Time.deltaTime);

        float entertainment = myBlackBoard.GetFloat("Entertainment");
        if (entertainment > 0)
            myBlackBoard.SetFloat("Entertainment", entertainment - personalizedEntertainment * Time.deltaTime);
    }
}