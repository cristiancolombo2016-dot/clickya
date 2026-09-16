#!/usr/bin/env python3
"""Genera un hash PBKDF2 compatible con PasswordSecurity sin mostrar la contraseña."""

import base64
import getpass
import hashlib
import os


password = getpass.getpass("Contraseña administrativa: ")
if len(password) < 12:
    raise SystemExit("Usá una contraseña de al menos 12 caracteres.")

salt = os.urandom(16)
digest = hashlib.pbkdf2_hmac("sha256", password.encode("utf-8"), salt, 210_000, 32)
print("PBKDF2-SHA256$210000$%s$%s" % (
    base64.b64encode(salt).decode("ascii"),
    base64.b64encode(digest).decode("ascii"),
))
