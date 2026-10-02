# بناء APK تلقائيًا — Real Driving Academy Stage 2

## ما تم تثبيته لتقليل احتمالات الفشل
- المشروع مضبوط على Unity 2022.3.62f1.
- `com.unity.ugui` مضافة صراحة لواجهة اللمس.
- مجلد المشهد يُنشأ تلقائيًا قبل حفظه.
- مجلد إخراج APK يُنشأ تلقائيًا.
- البناء يفشل برسالة واضحة إذا لم يُنتج APK حقيقيًا.
- GitHub Actions يتحقق من وجود `UNITY_LICENSE` قبل تنزيل/تشغيل Unity.
- المسار المتوقع ثابت: `Builds/Android/RealDrivingAcademy_Stage2.apk`.
- بعد البناء يتم إنشاء SHA-256 ورفع APK كـ Artifact.

## الأسرار المطلوبة في GitHub
في Repository Settings > Secrets and variables > Actions:
- `UNITY_LICENSE` — مطلوب.
- `UNITY_EMAIL` — يضاف إذا كان نوع تفعيل Unity لديك يحتاج البريد.
- `UNITY_PASSWORD` — يضاف إذا كان نوع التفعيل لديك يحتاج كلمة المرور.

لا تضع أي بيانات دخول داخل ملفات المشروع نفسها.

## التشغيل
Actions > Build Android APK > Run workflow

الناتج:
`RealDrivingAcademy-Stage2-APK`

ويحتوي:
- `RealDrivingAcademy_Stage2.apk`
- `RealDrivingAcademy_Stage2.sha256`

## ملاحظة مهمة
هذه النسخة Development APK موقعة بتوقيع التطوير الذي يستخدمه Unity، ومقصود بها التثبيت والاختبار. عند الوصول إلى نسخة النشر على Google Play سنستخدم keystore إصدار خاص بالمشروع ونبني AAB أيضًا.
