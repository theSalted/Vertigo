using InputAssets;
using UnityEngine;

public class RemoteControlCar : MonoBehaviour
{
    public GameObject player; // 玩家对象
    public GameObject car; // 遥控车对象
    public Camera playerCamera; // 玩家摄像机
    public Camera carCamera; // 遥控车摄像机
    public Collider controllerCollider; // 控制器的触发器
    private bool isControllingCar = false; // 是否在控制遥控车
    private bool isPlayerNearController = false; // 玩家是否接近控制器

    private FirstPersonController playerController; // 玩家控制脚本

    void Start()
    {
        playerController = player.GetComponent<FirstPersonController>();
        playerCamera.enabled = true;
        carCamera.enabled = false;
    }

    void Update()
    {
        // 检查是否按下E键并且玩家接近控制器
        if (isPlayerNearController && Input.GetKeyDown(KeyCode.E))
        {
            isControllingCar = !isControllingCar; // 切换控制权

            if (isControllingCar)
            {
                playerController.enabled = false; // 禁用玩家控制脚本
                playerCamera.enabled = false; // 禁用玩家摄像机
                carCamera.enabled = true; // 启用遥控车摄像机
            }
            else
            {
                playerController.enabled = true; // 启用玩家控制脚本
                playerCamera.enabled = true; // 启用玩家摄像机
                carCamera.enabled = false; // 禁用遥控车摄像机
            }
        }

        if (isControllingCar)
        {
            ControlCar();
        }
    }

    void ControlCar()
    {
        float move = Input.GetAxis("Vertical") * Time.deltaTime * 10.0f;
        float turn = Input.GetAxis("Horizontal") * Time.deltaTime * 50.0f;

        car.transform.Translate(0, 0, move);
        car.transform.Rotate(0, turn, 0);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == player)
        {
            isPlayerNearController = true; // 玩家接近控制器
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject == player)
        {
            isPlayerNearController = false; // 玩家远离控制器
        }
    }
}

