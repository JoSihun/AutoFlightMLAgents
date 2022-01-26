# Auto Flight ML-Agents
This is Drone Autonomous Flight using Unity ML-Agents.  
본 프로젝트는 Unity ML Agent를 활용한 LiDAR 기반 머신러닝을 이용한 드론 자율비행 시스템으로, 장애물(사물)을 회피하여 목적지까지 자율적으로 비행하는 시스템이다. LiDAR를 이용하여 장애물(사물)을 탐지하고, 탐지한 장애물(사물)의 거리정보를 기반으로 Unity ML Agent Package를 이용하여 PPO(Proximal Policy Optimization) 기반의 머신러닝을 수행하여, 드론 비행 시 발생하는 장애물에 대한 회피 알고리즘을 제안하고, 테스트환경에서 수행한 뒤 성능을 확인한다.
- [Unity ML-Agent](https://github.com/Unity-Technologies/ml-agents): https://github.com/Unity-Technologies/ml-agents


## 1. Environments

- Unity
- Unity ML Agent
- C# Script
- Anaconda3(Python 3.8.5)
- Window 10
- Intel Core i5-6600
- RAM DDR4-2133MHz(PC4-17000) 16GB
- NVIDIA GeForce GTX 1060 6GB

### 1. 1 Unity ML-Agents
- Download recent version of release branch at https://github.com/Unity-Technologies/ml-agents.  
- The location of 'ml-agents' is like below or free.  
```
ㄴroot
  ㄴAutoFlight  
  ㄴImages  
  ㄴml-agents  
```


## 2. Main Configuration

### 2. 1 Main Architecture
- This image shows how drone autonomous flight machine learning works.  
- Unity ML-Agents has 5 different functions below.  
**`Initialize`, `OnEpisodeBegin`, `CollectObservations`, `OnActionReceived`, `Heuristic`.**
- **`Heuristic`** is excluded because it is just checking function that if actions work or not.  

<p align="center"><img width="75%" src="Images/Architecture_001.jpg" /></p>

- First, get Environment Informations from `Environment` such as `Map Information`, `Target Position`, `Agent Position` etc.  
- Then, update the reward and modify the behavior to get better reward from `Unity ML Agent`.  
- During Learning, the learning information is transmitted to the `MonitoringUI`.  

### 2. 2 Sub Arhitecture
- This image shows how `Agent` learns from `Unity ML Agent`.

<p align="center"><img width="75%" src="Images/Architecture_002.jpg" /></p>

- A Behavior is selected automatically based on reward in Communiator from Unity ML Agent.  
- The Drone performs actions and detects obstacles by means of LiDAR sensors.  
- After determining whether the object detected by the sensor is an obstacle or a target,  
a reward is set according to the measured distance information.

### 2. 3 LiDAR
- This Autonomous Flight Simulation is based on LiDAR System.  

<p align="center"><img width="50%" src="Images/LiDAR_001.png" /></p>

- The light is emitted to the target and the reflected light is detected by the sensor around the light source.  
- Measure time of flight(ToF) for light to return.  
- Measure the distance(D) to the target using the constant speed of light(c).  

### 2. 4 Distance Reward

<p align="left"><img width="25%" src="https://user-images.githubusercontent.com/59362257/151125351-7080759b-cb8a-4309-8edb-c00e02df680e.png"/></p>

- Using Unity RayCast to implement LiDAR functionality.
- Normalize the measurement distance to a value of 0 to 1 by divided by the limit distance.
- If no object is detected within range, return -1.


## 3. Train

### 3. 1 Set Learning Environment
- Set ramdomly placed 5 cylinder-shaped obstales.  
- Set 100 Cube-shaped spaces of size 100m^3.  
- Distributed Reinforcement Learning 3,500,000 steps.  

<p align="center"><img width="75%" src="Images/Learning_001.png" /></p>

### 3. 2 AutoFlight.yaml
- Make `yaml` file like below.

<p align="center"><img width="25%" src="https://user-images.githubusercontent.com/59362257/151126806-635b9693-aaba-4d9d-a3c8-f150f7ab6116.png"/></p>

- Key Parameters
|Parameter Name|Description|
|:---|:---|
|batch_size|경사하강 1회 업데이트에 사용할 경험의 수|
|learning_rate|경사하강 학습의 정도|
|epsilon|이전 정책과 새 정책 사이의 비율 크기 제한|
|max_steps|학습할 총 step 수|

### 3. 3 Training
- After set learning environment, start machine learning with `Anaconda3` like below.  
- Start Training 3,500,000 Steps.
```anaconda3
~/ml-agents> mlagents-learn config/ppo/AutoFlight.yaml --run-id=AutoFlight
```  
<p align="center"><img width="75%" src="Images/Anaconda_001.png" /></p>
<p align="center"><img width="75%" src="Images/Anaconda_002.png" /></p>
<p align="center"><img width="75%" src="Images/Anaconda_003.png" /></p>
<p align="center"><img width="75%" src="Images/Anaconda_004.png" /></p>

### 3. 4 Training Result

<p align="center"><img width="100%" src="https://user-images.githubusercontent.com/59362257/151128795-5142eab1-a63d-49a8-95ce-63a94f90699f.png" /></p>

1,500,000 ~ 2,000,000 Step부터 보상이 증가하지 않음  
- Cumulative Reward  
모든 에이전트에 대한 평균 누적 에피소드 보상
- Extrinsic Reward  
에피소드당 환경에서 받는 평균 누적 보상
- Extrinsic Value Estimate  
에이전트가 방문한 모든 상태에 대한 평균값 추정치


## 4. Run

### 4. 1 TEST01
<p align="center"><img width="75%" src="Images/Test_001.jpg" /></p>

- Tried 100 times.
- Achieved about 24s Average Time.
- Achieved 96% Accuracy.

### 4. 2 TEST02
<p align="center"><img width="75%" src="Images/Test_002.png" /></p>

- Tried 100 times.
- Achieved about 24s Average Time.
- Achieved 86% Accuracy.

### 4. 4 Demo Simulation

<p align="center"><img width="75%" src="Images/Demo_001.png" /></p>
<p align="center"><img width="75%" src="Images/Demo_002.jpg" /></p>
<p align="center"><img width="75%" src="Images/Demo_003.jpg" /></p>

- Tried 100 times.
- Achieved about 11.5s Average Time.
- Achieved 89% Accuracy.

## 5. Results

### 5. 2 Result Analize
- Accuracy
- Benefit
- ETC
