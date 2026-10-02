# Real Driving Academy — Prototype 0.1

هذا هو الأساس البرمجي لأول نموذج قابل للعب للعبة تعليم القيادة الواقعية.

## ما تم بناؤه في هذه الحزمة
- فيزياء سيارة تعتمد على Rigidbody وWheelCollider.
- محرك RPM وعزم مرتبط بمنحنى عزم.
- ناقل أوتوماتيك ويدوي.
- غيار أمامي/محايد/خلفي.
- كلتش وانطفاء المحرك في الوضع اليدوي.
- ABS مبسط قابل للتطوير.
- Traction Control مبسط قابل للتطوير.
- فرامل خدمة وفرامل يد.
- مركز ثقل قابل للضبط لكل سيارة.
- اختلاف إعدادات المركبات عبر VehicleConfig.
- تشغيل المحرك وربط الحزام.
- إشارات يمين/يسار وتحذير رباعي.
- أنوار ومساحات.
- كاميرا سائق داخلية تتحرك يمين/يسار.
- نظام تعليم لأول درس.
- نظام نقاط ومخالفات.
- مناطق حدود سرعة.
- إشارة مرور تعمل بدورة زمنية.
- كشف تجاوز الإشارة الحمراء.
- HUD أولي للسرعة وRPM والغيار والنتيجة.

## 1) إنشاء مشروع Unity
أنشئ مشروع 3D جديد ثم انسخ مجلد:
`Assets/RealDrivingAcademy`
إلى مجلد Assets في مشروعك.

> في النموذج الحالي تستخدم لوحة المفاتيح للاختبار السريع. في Project Settings اجعل Active Input Handling يدعم Input Manager/كلا النظامين إذا كان مشروعك لا يقبل Input.GetAxis افتراضيًا. واجهة الهاتف ستستدعي دوال CarInputState مباشرة.

## 2) إعداد السيارة
أنشئ GameObject اسمه `TrainingSedan` وأضف إليه:
- Rigidbody
- CarInputState
- CarSystems
- RealisticCarController
- DrivingViolationSystem

أنشئ 4 WheelCollider:
- FrontLeftCollider
- FrontRightCollider
- RearLeftCollider
- RearRightCollider

اضبط أنصاف أقطار WheelCollider لتطابق الإطارات المرئية.

### نموذج دفع أمامي
في `RealisticCarController`:
- Front Axle: steering = true, powered = true
- Rear Axle: steering = false, powered = false, handbrake = true

## 3) VehicleConfig
في Project:
Create > Real Driving Academy > Vehicle Config

أنشئ `TrainingSedan_Config` ثم اربطه بـ RealisticCarController.

قيم بداية مقترحة:
- Mass: 1450 kg
- Max torque: 260 Nm
- Max steer: 34°
- Max speed: 190 km/h
- FWD

هذه الأرقام ليست مواصفات سيارة بعلامة تجارية؛ هي نقطة ضبط أولية للنموذج.

## 4) المقصورة الداخلية
أنشئ داخل السيارة:
- `DriverSeatAnchor` في موضع رأس السائق.
- Camera مع DriverCameraController.
- اربط DriverSeatAnchor.

للوصول للواقعية النهائية سنربط لاحقًا:
- المقود المرئي بزاوية WheelColliders.
- دواسات البنزين/الفرامل/الكلتش.
- عصا القير.
- عدادات Analog needles.
- مرايا RenderTexture.
- أزرار المقصورة بتفاعل لمس مباشر.

## 5) التحكم الحالي على الكمبيوتر
- W / Up: بنزين
- S / Down: فرامل
- A/D: توجيه
- Left Shift: كلتش
- Space: فرامل يد

تشغيل المحرك والإشارات والقير يمكن ربطها بأزرار UI تستدعي الدوال العامة في CarSystems وRealisticCarController.

## 6) أول درس
أضف GameObject اسمه `LessonManager` مع DrivingLessonManager واربط:
- car
- input
- systems

الدرس الافتراضي ينفذ:
1. ربط الحزام.
2. تشغيل السيارة.
3. اختيار الغيار.
4. فك فرامل اليد.
5. استخدام الإشارة اليسرى.
6. الوصول إلى 20 كم/س.
7. التوقف.

## 7) حدود السرعة
ضع BoxCollider كـ Trigger على بداية منطقة الطريق، ثم أضف `SpeedLimitZone` وحدد مثلًا 50 km/h.

## 8) الإشارة الحمراء
أنشئ TrafficLightController على مجسم الإشارة.
قبل خط التوقف ضع Collider Trigger مع `RedLightViolationZone` واربط الإشارة به.

## 9) واجهة الهاتف — المرحلة التالية
السكريبت `CarInputState` يحتوي بالفعل على:
- SetSteering
- SetThrottle
- SetBrake
- SetClutch
- SetHandbrake

وبذلك يمكن ربط المقود والدواسات اللمسية مباشرة دون تغيير فيزياء السيارة.

## 10) ما لم يُبنَ بعد
هذا نموذج برمجي أولي، وليس اللعبة النهائية. العناصر التالية هي المرحلة التالية مباشرة:
- موديل سيارة ومقصورة عالية التفاصيل.
- مرايا حقيقية RenderTexture محسنة للموبايل.
- تحريك المقود والقير والدواسات بصريًا.
- طريق تدريب 3D كامل.
- مواقف وCone Slalom.
- AI Traffic.
- مشاة.
- طقس ومطر وضباب.
- مدينة وطريق سريع وطريق جبلي.
- نظام احتكاك مستقل للأسفلت/المطر/الحصى.
- أصوات محرك متعددة الطبقات.
- نظام فحص النقطة العمياء.
- اختبار صف سيارات متكامل.
- واجهة عربية نهائية.

## ترتيب التطوير الذي يجب الحفاظ عليه
1. إحساس القيادة والفيزياء.
2. المقصورة والتفاعل الحقيقي.
3. التدريب والاختبارات.
4. الطريق والمرور.
5. الطقس والبيئة.
6. الرسوم النهائية والتوسعة إلى عدة سيارات.

الهدف هو ألا نضيف عشرات السيارات قبل أن تصبح أول سيارة ممتعة ومقنعة في القيادة.
