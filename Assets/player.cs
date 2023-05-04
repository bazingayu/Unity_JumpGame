using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
// using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using Random = UnityEngine.Random;
using UnityEngine.XR;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
using TMPro;
using UnityEditor;

public class player : MonoBehaviour
{
    // Start is called before the first frame update
    // decide how far it jump
    public float Factor;
    
    public float maxDistance;

    //the first object
    public GameObject Stage;
    
    public GameObject[] BoxTemplates;

    public GameObject[] rewards;

    public TMP_Text TotalScoreText;

    public Transform Head;

    // 小人身体
    public Transform Body;

    public TMP_Text SingleScoreText;
    public Transform board;

    private List<GameObject> StageList = new List<GameObject>();
    private Rigidbody _rigidbody;
    private GameObject _currentStage;
    private GameObject _newStage;
    private GameObject _reward;
    private Vector3 _cameraRalativePosition;
    private bool _direction_isX = true;
    private bool _enableInput = true;
    private bool _isPressed = false;
    private bool isAlive = true;
    private float _startTime;
    private Vector3 initialRelativePosition_camera;
    private Vector3 _boardRelativePosition;
    private bool isEnter = false;
    public GameObject Particle;

    private float initialVelocitY = 5f;
    private float _G = Mathf.Abs(Physics.gravity.y);
    Vector3 _direction = new Vector3(1, 0, 0);
    private int score = 0;
    private int totalScore = 0;
    public GameObject _xrRig;

    void Start()
    {
        Factor = maxDistance * 2.0f;
        if (isAlive)
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.transform.position = Vector3.zero;
            _rigidbody.velocity = Vector3.zero;
            _currentStage = Stage;
            SpawnStage();
            
            _cameraRalativePosition = Camera.main.transform.position - transform.position;
            _boardRelativePosition = board.position - transform.position;
            
            this.initialRelativePosition_camera = this._cameraRalativePosition;
        }
        else
        {
            _rigidbody.transform.position = Vector3.zero;
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.transform.eulerAngles = Vector3.zero;

            foreach (GameObject obj in this.StageList)
            {
                Destroy(obj);
            }
            this.StageList.Clear();

            _currentStage = Stage;
            SpawnStage();
            _cameraRalativePosition = this.initialRelativePosition_camera;
        }
        MoveCamera();
    }

    // Update is called once per frame
    void Update()
    {
        if (_enableInput)
        {
            // bool triggerButtonPressed = Input.GetAxis("XRI_Right_Trigger") > 0.5f;
            InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand); // 使用右手控制器
            bool triggerButtonPressed;
            device.TryGetFeatureValue(CommonUsages.triggerButton, out triggerButtonPressed); // 获取触发器按钮状态

            if (triggerButtonPressed && !_isPressed)
            {
                _startTime = Time.time;
                isEnter = true;
                _isPressed = true;
                Particle.SetActive(true);
            }

            if (!triggerButtonPressed && _isPressed)
            {
                if (isEnter)
                {
                    // 计算总共按下空格的时长
                    var elapse = Time.time - _startTime;
                    OnJump(elapse);

                    //还原小人的形状
                    Body.transform.DOScale(0.1f, 0.2f);
                    Head.transform.DOLocalMoveY(0.29f, 0.2f);

                    //还原盒子的形状
                    _currentStage.transform.DOLocalMoveY(-0.25f, 0.2f);
                    _currentStage.transform.DOScaleY(0.5f, 0.2f);
                    
                    isEnter = false;
                    _enableInput = false;
                    _isPressed = false;
                    Particle.SetActive(false);
                }
            }
            if (triggerButtonPressed)
            {
                if (isEnter)
                {
                    if (_currentStage.transform.localScale.y > 0.3)
                    {
                        Body.transform.localScale += new Vector3(1, -1, 1) * 0.05f * Time.deltaTime;
                        Head.transform.localPosition += new Vector3(0, -1, 0) * 0.1f * Time.deltaTime;

                        _currentStage.transform.localScale += new Vector3(0, -1, 0) * 0.15f * Time.deltaTime;
                        _currentStage.transform.localPosition += new Vector3(0, -1, 0) * 0.15f * Time.deltaTime;
                    }
                }
            }
            
            
            
            
            if (Input.GetMouseButtonDown(0))
            {
                _startTime = Time.time;
                isEnter = true;
                Particle.SetActive(true);
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (isEnter)
                {
                    // 计算总共按下空格的时长
                    var elapse = Time.time - _startTime;
                    OnJump(elapse);

                    //还原小人的形状
                    Body.transform.DOScale(0.1f, 0.2f);
                    Head.transform.DOLocalMoveY(0.29f, 0.2f);

                    //还原盒子的形状
                    _currentStage.transform.DOLocalMoveY(-0.25f, 0.2f);
                    _currentStage.transform.DOScaleY(0.5f, 0.2f);
                    
                    isEnter = false;
                    _enableInput = false;
                    Particle.SetActive(false);
                }
            }
            
            //Squash the avatar when press the button
            if (Input.GetMouseButton(0))
            {
                if (isEnter)
                {
                    if (_currentStage.transform.localScale.y > 0.3)
                    {
                        Body.transform.localScale += new Vector3(1, -1, 1) * 0.05f * Time.deltaTime;
                        Head.transform.localPosition += new Vector3(0, -1, 0) * 0.1f * Time.deltaTime;

                        _currentStage.transform.localScale += new Vector3(0, -1, 0) * 0.15f * Time.deltaTime;
                        _currentStage.transform.localPosition += new Vector3(0, -1, 0) * 0.15f * Time.deltaTime;
                    }
                    
                }
            }
        }
    }

    void OnJump(float elapse)
    {
        // t = v / a
        Debug.Log("elapse: " + elapse + ", Factor: " + Factor);
        float t = 2 * initialVelocitY / _G;
        Vector3 _direction_fix;
        if (this._direction_isX)
        {
            //v = s / t
            float velocityX = (this._newStage.transform.position.x - this._rigidbody.transform.position.x) / t;
            // I = m * a
            float forceX = velocityX * _rigidbody.mass;
            _direction_fix = new Vector3(forceX, 0, 0);
        }
        else
        {
            float velocityZ = (this._newStage.transform.position.z - this._rigidbody.transform.position.z) / t;
            // I = m * a
            float forceZ = velocityZ * _rigidbody.mass;
            _direction_fix = new Vector3(0, 0, forceZ);
        }
        // I = m * a
        Debug.Log("befor jump: " + this.initialVelocitY * this._rigidbody.mass);
        _rigidbody.AddForce(new Vector3(0, this.initialVelocitY * this._rigidbody.mass, 0) + (_direction) * Mathf.Log(1 + elapse) * Factor + _direction_fix, ForceMode.Impulse);
        
        Debug.Log("Force: " + (_direction) * Mathf.Log(1 + elapse) * Factor);
        
        transform.DOLocalRotate(new Vector3(0, 0, -360), 0.6f, RotateMode.LocalAxisAdd);
    }
    
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log(collision.gameObject.name);
        if (collision.gameObject.name == "Ground")
        {
            totalScore = Math.Max(score, totalScore);
            score = 0;
            TotalScoreText.text = "highest score: " + totalScore;
            SingleScoreText.text = "Score: " + score;
            this.isAlive = false;
            this.Start();
        }
        else
        {

            if (_currentStage != collision.gameObject)
            {
                Debug.Log("currentStage != gameObeject ==>" + collision.gameObject.name);
                
                var contacts = collision.contacts;

                //check if player's feet on the stage
                if (contacts.Length == 1 && contacts[0].normal == Vector3.up)
                {

                    _currentStage = collision.gameObject;
                    RandomDirection();
                    SpawnStage();
                    MoveCamera();

                    _enableInput = true;
                    score += 1;
                    SingleScoreText.text = "Score: " + score;
                    
                }
                else // body collides with the box
                {
                    _enableInput = true;
                    

                }
            }
            else //still on the same box
            {
                var contacts = collision.contacts;

                //check if player's feet on the stage
                if (contacts.Length == 1 && contacts[0].normal == Vector3.up)
                {
                    _enableInput = true;
                }
            }
        }
    }
    void RandomDirection()
    {
        var seed = Random.Range(0, 2);
        _direction = seed == 0 ? new Vector3(1, 0, 0) : new Vector3(0, 0, 1);
        this._direction_isX = seed == 0 ? false : true;
        transform.right = _direction;
    }

    /// <summary>
    /// 移动摄像机
    /// </summary>
    void MoveCamera()
    {
        _xrRig.transform.DOMove(transform.position + _cameraRalativePosition, 1);
        int len = Camera.allCameras.Length;
        for (int i = 0; i < len; i++)
        {
            Camera.allCameras[i].transform.DOMove(transform.position + _cameraRalativePosition, 1);
        }
        // Camera.main.transform.DOMove(transform.position + _cameraRalativePosition, 1);
        board.transform.DOMove((transform.position + _boardRelativePosition), 1);
    }
    
    void SpawnStage()
    {
        GameObject prefab;
        GameObject rewardPrefab;
        if (BoxTemplates.Length > 0)
        {
            // 从盒子库中随机取盒子进行动态生成
            prefab = BoxTemplates[Random.Range(0, BoxTemplates.Length)];
            rewardPrefab = rewards[Random.Range(0, rewards.Length)];
        }
        else
        {
            prefab = Stage;
            rewardPrefab = null;
        }

        if (this._reward != null)
        {
            Destroy(this._reward);
        }
        
        var stage = Instantiate(prefab);
        var rewardStage = Instantiate(rewardPrefab);

        var randomScale = Random.Range(0.5f, 1);
        stage.transform.localScale = new Vector3(1f, 0.5f, 1f);
        stage.transform.localPosition = new Vector3(stage.transform.position.x, -0.3f, stage.transform.position.z);
        // _currentStage.transform.DOScaleY(0.5f, 0.2f);
        
        stage.transform.position = _currentStage.transform.position + _direction * Random.Range(1.1f, maxDistance);
        if (this._direction_isX)
        {
            // rewardStage.transform. = new Vector3(30f, 30f, 30f);
            // rewardStage.transform.localPosition = new Vector3(stage.transform.position.x, -0.3f, stage.transform.position.z -15.0f);
            // _currentStage.transform.DOScaleY(0.5f, 0.2f);
        
            rewardStage.transform.position = _currentStage.transform.position + _direction * Random.Range(1.1f, maxDistance) + new Vector3(0,-0.25f,-5f);
            rewardStage.transform.DOLocalRotate(new Vector3(_currentStage.transform.position.x, 360, _currentStage.transform.position.z), 5f, RotateMode.LocalAxisAdd);
        }
        else
        {
            // rewardStage.transform.localScale = new Vector3(30f, 30f, 30f);
            // rewardStage.transform.localPosition = new Vector3(stage.transform.position.x - 5.0f, -0.3f, stage.transform.position.z);
            // _currentStage.transform.DOScaleY(0.5f, 0.2f);
        
            rewardStage.transform.position = _currentStage.transform.position + _direction * Random.Range(1.1f, maxDistance) + new Vector3(-5f, -0.25f, 0);
            rewardStage.transform.DOLocalRotate(new Vector3(_currentStage.transform.position.x, 360, _currentStage.transform.position.z), 5f, RotateMode.LocalAxisAdd);
        }

        this._reward = rewardStage;

        // 重载函数 或 重载方法
        stage.GetComponent<Renderer>().material.color =
            new Color(Random.Range(0f, 1), Random.Range(0f, 1), Random.Range(0f, 1));
        this._newStage = stage;
        this.StageList.Add(stage);
    }
}
