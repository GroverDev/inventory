import java.util.Properties
import java.io.FileInputStream

plugins {
    id("com.android.application")
    // The Flutter Gradle Plugin must be applied after the Android Gradle plugin.
    id("dev.flutter.flutter-gradle-plugin")
}

// Credenciales de la clave de subida. El archivo NO se versiona (está en
// .gitignore): cada máquina que publique debe tener el suyo. Ver RELEASE.md.
val keystorePropertiesFile = rootProject.file("key.properties")
val keystoreProperties = Properties()
if (keystorePropertiesFile.exists()) {
    keystoreProperties.load(FileInputStream(keystorePropertiesFile))
}
val hasUploadKey = keystorePropertiesFile.exists()

android {
    namespace = "com.ideanueva.puntoventa"
    compileSdk = flutter.compileSdkVersion
    ndkVersion = flutter.ndkVersion

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    kotlinOptions {
        jvmTarget = JavaVersion.VERSION_17.toString()
    }

    defaultConfig {
        // Identificador definitivo en Google Play. NO se puede cambiar una vez
        // publicada la primera versión: cambiarlo crea una app distinta, sin
        // los usuarios ni las reseñas de la anterior.
        applicationId = "com.ideanueva.puntoventa"
        minSdk = flutter.minSdkVersion
        targetSdk = flutter.targetSdkVersion
        versionCode = flutter.versionCode
        versionName = flutter.versionName
    }

    signingConfigs {
        create("release") {
            if (hasUploadKey) {
                keyAlias = keystoreProperties["keyAlias"] as String
                keyPassword = keystoreProperties["keyPassword"] as String
                storeFile = file(keystoreProperties["storeFile"] as String)
                storePassword = keystoreProperties["storePassword"] as String
            }
        }
    }

    buildTypes {
        release {
            // Sin key.properties se firma con la clave de debug, para que
            // `flutter run --release` siga funcionando en una máquina de
            // desarrollo. Ese artefacto NO sirve para publicar: Play rechaza
            // todo lo firmado con la clave de debug.
            signingConfig = if (hasUploadKey) {
                signingConfigs.getByName("release")
            } else {
                signingConfigs.getByName("debug")
            }
        }
    }
}

flutter {
    source = "../.."
}
