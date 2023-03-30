//using ResourceNamespace;
using System;
using UnityEngine;
using static Module;

public class InteractionModule : MonoBehaviour
{
    public bool isMulti = true;

    private PlayerInput playerInput;
    private Animator playerAnimator;
    private InteractionObject interactionObject;

    //private ResourceChanger resourceChanger;
    private Spaceship spaceship;
    private GameObject player;

    // Edge 체크를 위한 오브젝트
    private GameObject matchObject;
    private GameObject targetObject;

    // Resource 변경을 위한 오브젝트
    private GameObject resourceObject;

    // 맞은 모듈 확인
    private Module struckModule;

    // player 위치
    private Vector3 playerPosition;

    // 멀티를 위한 오브젝트
    public MultiSpaceship multiSpaceship;

    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        playerAnimator = GetComponent<Animator>();
        interactionObject = GetComponent<InteractionObject>();

        spaceship = FindAnyObjectByType<Spaceship>();

        multiSpaceship = GameObject.Find("Server").GetComponent<MultiSpaceship>();

        player = GameObject.Find("PlayerCharacter");
        playerPosition = player.GetComponent<Transform>().position;

    }

    private void OnTriggerEnter(Collider other)
    {
        if (!interactionObject.isHoldingObject)
        {
            if (other.gameObject.CompareTag("Edge"))
            {
                matchObject = other.gameObject;
                Module module = matchObject.GetComponentInParent<Module>();

                int idxZ = module.idxZ;
                int idxX = module.idxX;

                switch (other.gameObject.name)
                {
                    case "EdgeTop":
                        idxZ += 1;
                        break;
                    case "EdgeBottom":
                        idxZ -= 1;
                        break;
                    case "EdgeRight":
                        idxX += 1;
                        break;
                    case "EdgeLeft":
                        idxX -= 1;
                        break;
                }

                targetObject = spaceship.modules[idxZ, idxX];
                targetObject.GetComponent<Module>().floorModule.SetActive(true);
            }
        }

        if (other.gameObject.CompareTag("Change"))
        {
            resourceObject = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!interactionObject.isHoldingObject && targetObject != null)
        {
            Module module = targetObject.GetComponentInParent<Module>();

            if (module.moduleType == ModuleType.Blueprint)
            {
                targetObject.GetComponent<Module>().floorModule.SetActive(false);
            }

            matchObject = null;
            targetObject = null;
        }
        else if (resourceObject != null)
        {
            resourceObject = null;
        }

    }

    private void Update()
    {
        if (playerInput.Interact)
        {
            if (matchObject != null && targetObject != null)
            {
                if (targetObject.GetComponent<Module>().moduleType == ModuleType.Blueprint)
                {
                    if (isMulti)
                    {
                        Module module = targetObject.GetComponent<Module>();
                        Debug.Log("space" + module.idxX + " , " + module.idxZ + " , " + (int)ModuleType.LaserTurret);
                        multiSpaceship.SendCreateModule(module.idxX, module.idxZ, (int)ModuleType.LaserTurret);    // 바닥생성
                    }
                    else
                    {
                        targetObject.GetComponent<Module>().CreateFloor(ModuleType.LaserTurret);
                        spaceship.MakeWall(targetObject);
                    }
                }
            }
            else if (resourceObject != null)
            {
                //resourceObject.GetComponent<ResourceChanger>().SwitchResource();
            }
        }

        if (playerInput.RepairModule)
        {
            playerPosition = player.GetComponent<Transform>().position;

            int playerX = (int)(Math.Round(playerPosition.x / 5) + 10);
            int playerZ = (int)(Math.Round(playerPosition.z / 5) + 10);

            struckModule = spaceship.modules[playerZ, playerX].GetComponent<Module>();

            playerAnimator.SetBool("Repairing", true);

            if (struckModule.hp < 3)
            {
                struckModule.hp += 0.1f;
            }
        }
        else
        {
            playerAnimator.SetBool("Repairing", false);
        }
    }
}