# Line Follower Robot — SpeedyBee

<img src="https://github.com/user-attachments/assets/c00e1fcc-1e4a-4ed0-aeda-4e18eb7595ab" 
     alt="Team Logo" 
     width="200" 
     height="200"
     style="object-fit: cover; border-radius: 10px; float: left; margin-right: 20px;" />


---

## Overview

This project is a simple implementation of a **line follower robot**, developed for **RoboChallenge 2025**.  
The robot uses an **IMU (Inertial Measurement Unit)** together with line sensors to follow a marked path.  
It was designed with **low-cost components** to demonstrate efficient motion control within a limited budget.

A companion application was also developed to **visualize the robot’s path in real time** based on IMU data.  
This information is used for **PD controller tuning**, helping to achieve better path tracking and stability.

---

## Robot Overview

![SpeedyBee Robot](https://github.com/user-attachments/assets/4fdc509f-0c2a-4c5f-b23e-999838ac6c9e)


*Figure 1. SpeedyBee Line Follower Robot PCB prototypes.*

---

## System Diagram

![System Diagram](https://github.com/user-attachments/assets/d4b6568a-5f8d-4a72-830c-bdbf6bcb5c84)

*Figure 2. Block diagram showing system architecture and data flow.*

---

## Key Features

- Line following control using a PD algorithm  
- Real-time IMU data visualization through a companion app  
- Tuned for stable and accurate movement during competition  

---

## Components

| Component | Description | Purpose |
|------------|-------------|----------|
| **Microcontroller (Marble Pico)** | Main controller handling sensors and motor control | Central processing unit |
| **Motor Driver (DRV8243)** | Single H-bridge driver | Controls motor speed and direction |
| **Battery Pack** | 3S LiPo battery | Power supply |
| **IMU Sensor (Bosch BMI 323)** | Gyroscope and accelerometer module | Provides motion and orientation data |
| **IR Sensor Array (JSumo XLine 16)** | Reflective sensors | Detects the path line |
| **DC Motors (NovaMax 450 RPM)** | Drive system | Movement |

---

## Future Improvements

- Improve power efficiency and battery management  
- Expand visualization features (e.g., web or PC interface)  

---

## Acknowledgements

This project was developed for **RoboChallenge 2025** as a practical implementation of control systems and sensor fusion.  
The focus was on building a **functional, affordable, and tunable robot** using easily available components.

---

## Authors

- [Benjámin Papp](https://github.com/PappBenjamin)  
- [Csongor Kántor](https://github.com/progenor)
- [Hanga Barabás](https://github.com/bgabigel)
