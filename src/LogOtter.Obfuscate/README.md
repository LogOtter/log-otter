﻿# Obfuscate

A library to obfuscate certain types of strings.

## Usage

### Email

```c#
Obfuscate.Email("bob.bobertson@gmail.com"); // bo****n@gmail.com
Obfuscate.Email("bob@gmail.com"); // b****@gmail.com
```

### Phone

```c#
Obfuscate.Phone("07770123456"); // 07****56
```

### Name

```c#
Obfuscate.Name("Bob", "Bobertson"); // B**** Bo****
```
