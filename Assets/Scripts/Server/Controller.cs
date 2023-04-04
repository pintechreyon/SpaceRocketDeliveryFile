using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum PacketType
{
    NONE,
    HELLO,
    BYE,
    CREATE_ROOM,
    PARTICIPATE_USER, // 유저가 방에 입장한다는 것
    DEPARTURE_USER,
    PARTICIPATE_ROOM, // 방안에 있는 유저목록을 반환
    MOVE,
    MODULE_CONTROL,
    REPLICATION,

    OBJECT_MOVE,
    OBJECT_CONTROL, 
    
    MODULE_STATUS,
    CURRENT_POSITION,
    //ENEMY_MOVE,
    TURRET_STATUS,
    BASIC_TURRET,

    ENEMY_MOVE = 199, // 적 생성
    MODULE_CREATE = 200, // 모듈 생성
    SUPPLIER_CREATE = 210, // SUPPLIER 오브젝트 생성
    SUPPLIER_CHANGE = 211, // SUPPLIER 오브젝트 변경
    RESOURCE_MOVE = 212, // 리소스 움직임

    ENEMY_CREATE = 220, // 적 생성
    
};

public class DTOuser    // 유저 방 Enter 이후
{
    public string roomName;
    public int userId;
    public float x, y, z, r;
    public string userNickName;
}

public class DTOresourcemove    // 자원 움직임
{
    public int idxR;
    public float px, py, pz;
    public float rx, ry, rz, rw;
}

public class DTOenemymove    // 적 움직임
{
    public int idxE;
    public int type;
    public float px, py, pz;
    public float rx, ry, rz, rw;
}

public class Controller : MonoBehaviour
{
    
    CreateRoomController createRoomController;      // 방 생성을 위한 컨트롤러
    EnterRoomController enterRoomController;        // 방 참가를 위한 컨트롤러
    CreateModuleController createModuleController;  // 모듈 추가를 위한 컨트롤러
    CreateResourceController createResourceController; // 자원 추가를 위한 컨트롤러
    MoveResourceController moveResourceController;  // 자원 위치를 위한 컨트롤러
    MoveEnemyController moveEnemyController;
    // 포지션 변경을 위한 변수
    PlayerPositionController playerPositionController;

    // Player관련 함수
    Multiplayer multiplayer;
    MultiSpaceship multiSpaceship;
    MultiEnemy multiEnemy;

    SocketClient socketClient;

    void Start()
    {
        socketClient = GetComponent<SocketClient>();

        // 필요한 컨트롤러 인스턴스 생성.
        createRoomController = new CreateRoomController();
        enterRoomController = new EnterRoomController();
        playerPositionController = new PlayerPositionController();
        createModuleController = new CreateModuleController();
        createResourceController = new CreateResourceController();
        moveResourceController = new MoveResourceController();
        moveEnemyController = new MoveEnemyController();

        // 멀티플레이 관련 로직 
        multiplayer = GetComponent<Multiplayer>();
        multiSpaceship = GetComponent<MultiSpaceship>();
        multiEnemy = GetComponent<MultiEnemy>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        createRoomController.Service();
        enterRoomController.Service();
        playerPositionController.Service(multiplayer);
        createModuleController.Service(multiSpaceship);
        createResourceController.Service(multiSpaceship);
        moveResourceController.Service();
        moveEnemyController.Service();
    }

    public void Receive(PacketType header, byte[] data)
    {
        try
        {
            switch (header)
            {
                case PacketType.CREATE_ROOM:
                    
                    createRoomController.ReceiveDTO(data);
                    createRoomController.SetAct(true);
                    Debug.Log("방생성 수신");
                    
                    break;
                case PacketType.PARTICIPATE_ROOM:
                    byte[] isSucess = SplitArray(data, 0, 1);
                    byte[] userCount = SplitArray(data, 1, 4);
                    int head = 5;
                    for (int i = 0; i < BitConverter.ToInt32(userCount, 0); i++)
                    {
                        DTOuser user = new DTOuser();
                        enterRoomController.newReceiveDTO(data, user, ref head);

                        Debug.Log(user.userNickName);
                        Debug.Log(user.userId);
                        Debug.Log(user.roomName);
                        Debug.Log("--------");
                    }
                    //enterRoomController.ReceiveDTO(data);
                    enterRoomController.SetAct(true);
                    break;
                case PacketType.MOVE:
                    playerPositionController.ReceiveDTO(data);
                    playerPositionController.SetAct(true);
                    break;
                case PacketType.MODULE_CREATE:
                    createModuleController.ReceiveDTO(data);
                    createModuleController.SetAct(true);
                    break;
                case PacketType.SUPPLIER_CREATE:
                    createResourceController.ReceiveDTO(data);
                    createResourceController.SetAct(true);
                    break;
                case PacketType.RESOURCE_MOVE:
                    try
                    {
                        byte[] resourceCount = SplitArray(data, 0, 4);
                        DTOresourcemove[] resourceList = new DTOresourcemove[BitConverter.ToInt32(resourceCount, 0)];
                        int head2 = 4;
                        for (int i = 0; i < BitConverter.ToInt32(resourceCount, 0); i++)
                        {
                            DTOresourcemove resource = new DTOresourcemove();
                            moveResourceController.newReceiveDTO(data, resource, ref head2);
                            resourceList[i] = resource;
                        }
                        multiSpaceship.ReceiveMoveResource(resourceList);
                        moveResourceController.SetAct(true);
                    }
                    catch(Exception e)
                    {
                        Debug.LogException(e);
                    }
                    break;
                case PacketType.ENEMY_MOVE:
                    Debug.Log("ENEMY_MOVE");
                    if (true)//multiplayer.isHost == false
                    {
                        try
                        {
                            byte[] enemyCount = SplitArray(data, 0, 4);
                            DTOenemymove[] enemyList = new DTOenemymove[BitConverter.ToInt32(enemyCount, 0)];
                            int head3 = 4;
                            Debug.Log(BitConverter.ToInt32(enemyCount, 0));
                            for (int i = 0; i < BitConverter.ToInt32(enemyCount, 0); i++)
                            {
                                DTOenemymove resource = new DTOenemymove();
                                moveEnemyController.newReceiveDTO(data, resource, ref head3);
                                enemyList[i] = resource;
                            }
                            multiEnemy.ReceiveMoveEnemy(enemyList);
                            moveEnemyController.SetAct(true);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    public void Send(PacketType header, params object[] args)      // 인자를 object배열로 받아옴
    {
        List<byte> byteList = new List<byte>();             // List를 byte로 받아옴


        // header 세팅. header를 해석하면 뒷단 정보 구조를 제공받을 수 있음
        byteList.Add((byte)header); // BitConverter.GetBytes()

        // params 직렬화
        for (int i = 0; i < args.Length; i++)
        {
            object arg = args[i];
            Type type = arg.GetType();

            if (type.Equals(typeof(int)))
            {
                byteList.AddRange(BitConverter.GetBytes((int)arg));
            }
            else if (type.Equals(typeof(float)))
            {
                byteList.AddRange(BitConverter.GetBytes((float)arg));
            }
            else if (type.Equals(typeof(double)))
            {
                byteList.AddRange(BitConverter.GetBytes((double)arg));
            }
            else if (type.Equals(typeof(string))) 
            {
                byte[] stringBytes = Encoding.UTF8.GetBytes((string)arg); // 문자열을 바이트 배열로 변환
                byteList.AddRange(BitConverter.GetBytes(stringBytes.Length)); // 문자열 바이트 길이를 먼저 추가
                byteList.AddRange(stringBytes); // 문자열 바이트 배열 추가
            }
        }
        byte[] byteArray = byteList.ToArray();
        // 전송 시작
        socketClient.Send(byteArray);
    }

    public void ListSend(PacketType header, List<object> args)      // 인자를 object배열로 받아옴
    {
        List<byte> byteList = new List<byte>();             // List를 byte로 받아옴


        // header 세팅. header를 해석하면 뒷단 정보 구조를 제공받을 수 있음
        byteList.Add((byte)header); // BitConverter.GetBytes()

        // params 직렬화
        for (int i = 0; i < args.Count; i++)
        {
            object arg = args[i];
            Type type = arg.GetType();

            if (type.Equals(typeof(int)))
            {
                byteList.AddRange(BitConverter.GetBytes((int)arg));
            }
            else if (type.Equals(typeof(float)))
            {
                byteList.AddRange(BitConverter.GetBytes((float)arg));
            }
            else if (type.Equals(typeof(double)))
            {
                byteList.AddRange(BitConverter.GetBytes((double)arg));
            }
            else if (type.Equals(typeof(string)))
            {
                byte[] stringBytes = Encoding.UTF8.GetBytes((string)arg); // 문자열을 바이트 배열로 변환
                byteList.AddRange(BitConverter.GetBytes(stringBytes.Length)); // 문자열 바이트 길이를 먼저 추가
                byteList.AddRange(stringBytes); // 문자열 바이트 배열 추가
            }
        }
        byte[] byteArray = byteList.ToArray();
        // 전송 시작
        //Debug.Log("보낸다: " + header);
        socketClient.Send(byteArray);
    }

    public byte[] SplitArray(byte[] array, int startIndex, int length)
    {
        byte[] result = new byte[length];
        Array.Copy(array, startIndex, result, 0, length);
        return result;
    }
}

// 유저 움직임
public class PlayerPositionController : ReceiveController
{
    public int userId;
    public float px;
    public float py;
    public float pz;
    public float rx;
    public float ry;
    public float rz;
    public float rw;

    public void Service(Multiplayer multiplayer) // isAct가 활성화 되었을 때 실행할 로직
    {
        if (this.GetAct())
        {
            multiplayer.MoveOtherPlayer(userId, px, py, pz, rx, ry,rz, rw);
            this.SetAct(false);
        }
    }
}

// CreateRoomController
public class CreateRoomController : ReceiveController
{
    public string text;      // 텍스트
    public string text2;      // 텍스트

    public new void Service() // isAct가 활성화 되었을 때 실행할 로직
    {
        if (this.GetAct())
        {
            // 여기에서 방이름을 로그로만 띄운 후
            Debug.Log(text);
            Debug.Log(text2);

            this.SetAct(false);
        }
    }
}

// EnterRoomController
public class EnterRoomController : ReceiveController
{
    

    public new void Service() // isAct가 활성화 되었을 때 실행할 로직
    {
        if (this.GetAct())
        {
            Debug.Log("여기 도착?");
            
            this.SetAct(false);
        }
    }
}

// 모듈 컨트롤러
public class CreateModuleController : ReceiveController
{
    public int xIdx;           // 위치
    public int zIdx;

    public int moduleType;     // 모듈 타입

    public void Service(MultiSpaceship multiSpaceship) // isAct가 활성화 되었을 때 실행할 로직
    {
        if (this.GetAct())
        {
            Debug.Log("CreateModuleController : " + xIdx +", "+ zIdx + ", " + moduleType);
            multiSpaceship.ReceiveCreateModule(xIdx, zIdx, moduleType);
            this.SetAct(false);
        }
    }
}

// 자원 생성
public class CreateResourceController : ReceiveController
{
    public int rIdx;
    public void Service(MultiSpaceship multiSpaceship) // isAct가 활성화 되었을 때 실행할 로직
    {
        if (this.GetAct())
        {
            Debug.Log("CreateResourceController : 자원 생성");
            multiSpaceship.ReceiveCreateResource(rIdx);
            this.SetAct(false);
        }
    }
}

// 자원 변경
public class ChangeResourceController : ReceiveController
{
    public int rIdx;
    public void Service(MultiSpaceship multiSpaceship) // isAct가 활성화 되었을 때 실행할 로직
    {
        if (this.GetAct())
        {
            Debug.Log("ChangeResourceController : 자원 변경");
            multiSpaceship.ReceiveChangeResource();
            this.SetAct(false);
        }
    }
}

// 자원 움직임
public class MoveResourceController : ReceiveController
{
    public void Service(MultiSpaceship multiSpaceship) // isAct가 활성화 되었을 때 실행할 로직
    {
        if (this.GetAct())
        {
            Debug.Log("MoveResourceController : 자원 위치 변경");
            multiSpaceship.ReceiveChangeResource();
            this.SetAct(false);
        }
    }
}

// 적 움직임
public class MoveEnemyController : ReceiveController
{
    public new void Service() // isAct가 활성화 되었을 때 실행할 로직
    {
        if (this.GetAct())
        {
            //multiSpaceship.ReceiveChangeResource();
            this.SetAct(false);
        }
    }
}

// 컨트롤러 정의
public class ReceiveController
{

    static public byte[] SplitArray(byte[] array, int startIndex, int length)
    {
        byte[] result = new byte[length];
        Array.Copy(array, startIndex, result, 0, length);
        return result;
    }

    private bool isAct = false;     // 활성화 되어있으면 실행시킨다.

    public void ReceiveDTO(byte[] data) // 데이터를 받으면 역직렬화 후 Class에 맞는 데이터로 변형시킨다.
    {
        Type typeClass = this.GetType();
        FieldInfo[] fields = typeClass.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); // 이 클래스를 참조하여 필요한 필드를 찾는다.

        int n = 0;
        foreach (FieldInfo field in fields) // 필드별로 돌며 역직렬화 한다.
        {

            Type type = field.FieldType;

            if (type.Equals(typeof(int)))
            {

                byte[] result = new byte[sizeof(int)];
                Array.Copy(data, n, result, 0, sizeof(int));
                field.SetValue(this, BitConverter.ToInt32(result));
                n += sizeof(int);
            }
            else if (type.Equals(typeof(float)))
            {
                byte[] result = new byte[sizeof(float)];
                Array.Copy(data, n, result, 0, sizeof(float));
                field.SetValue(this, BitConverter.ToSingle(result));
                n += sizeof(float);
            }
            else if (type.Equals(typeof(double)))
            {
                byte[] result = new byte[sizeof(double)];
                Array.Copy(data, n, result, 0, sizeof(double));
                field.SetValue(this, BitConverter.ToDouble(result));
                n += sizeof(double);
            }
            else if (type.Equals(typeof(string))) // 추가된 부분
            {
                
                byte[] stringLengthBytes = new byte[sizeof(int)];
                Array.Copy(data, n, stringLengthBytes, 0, sizeof(int));
                int stringLength = BitConverter.ToInt32(stringLengthBytes);
                n += sizeof(int);
                
                Debug.Log("스트링 렝스" + stringLength);
                byte[] stringBytes = new byte[stringLength];
                Array.Copy(data, n, stringBytes, 0, stringLength);
                
                n += stringLength;
                
                string stringValue = Encoding.UTF8.GetString(stringBytes);
                field.SetValue(this, stringValue);

                Debug.Log(stringValue);
                

            }
        }
    }

    public void newReceiveDTO<T>(byte[] data, T c, ref int mHead)
    {
        Type type = typeof(T);
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {
            Type t = field.FieldType;
            if (t.Equals(typeof(int)))
            {
                //Console.WriteLine("인트형");

                //배열 헤드부터 4바이트 만큼 자름
                byte[] intByte = SplitArray(data, mHead, 4);
                field.SetValue(c, BitConverter.ToInt32(intByte));

                //헤드 올림
                mHead += sizeof(int);
                //field.SetValue(c, 100);
            }
            else if (t.Equals(typeof(float)))
            {
                //Console.WriteLine("실수형");

                //배열 헤드부터 4바이트 만큼 자름
                byte[] floatByte = SplitArray(data, mHead, 4);
                field.SetValue(c, BitConverter.ToSingle(floatByte));

                //헤드 올림
                mHead += sizeof(float);
                //field.SetValue(c, 100);
            }
            else if (t.Equals(typeof(string)))
            {
                //Console.WriteLine("문자열");
                //4바이트만큼 자름
                byte[] intByte = SplitArray(data, mHead, 4);
                int size = BitConverter.ToInt32(intByte);
                mHead += sizeof(int);


                //앞서 얻은 크기만큼 배열을 자른다.

                byte[] stringByte = SplitArray(data, mHead, size);
                string stringValue = Encoding.UTF8.GetString(stringByte);
                field.SetValue(c, stringValue);

                mHead += size;
            }
        }

    }

   
    public bool GetAct()
    {
        return isAct;
    }

    public void SetAct(bool b)
    {
        isAct = b;
    }

    public void Service() { }
}