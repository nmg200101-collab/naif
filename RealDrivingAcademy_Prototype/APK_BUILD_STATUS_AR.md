# حالة بناء APK

تم تجهيز المشروع ليُخرج APK تلقائياً من خلال `AndroidBuild.cs`، وتمت إضافة سكربتي بناء لويندوز ولينكس.

## لماذا لا يوجد APK داخل هذه الحزمة؟
بيئة التنفيذ التي أنشئ فيها المشروع لا تحتوي على Unity Editor ولا Android SDK/Build Support، وقد تم فحصها فعلياً. لذلك لا يمكن تنفيذ عملية Unity Build هنا دون محرك Unity نفسه.

## عند وجود Unity
- ثبّت Unity 2022.3 LTS أو إصدار متوافق مع Android Build Support.
- افتح المشروع مرة واحدة.
- اختر `Real Driving Academy > Build Android APK`.
- أو شغّل `BUILD_ANDROID_WINDOWS.bat` / `BUILD_ANDROID_LINUX.sh`.
- الناتج: `Builds/Android/RealDrivingAcademy_Stage2.apk`.

هذه ليست مشكلة في ملفات اللعبة؛ إنها غياب برنامج البناء من بيئة التنفيذ الحالية.
