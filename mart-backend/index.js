const path = require("path");
const fs = require("fs");
const multer = require("multer");
require("dotenv").config();

const bcrypt = require("bcrypt");
const jwt = require("jsonwebtoken");
const { PrismaClient } = require("@prisma/client");
const express = require("express");

const prisma = new PrismaClient();
const app = express();
const PORT = process.env.PORT || 3001;
const JWT_SECRET = process.env.JWT_SECRET || "mart-dev-secret-change-me";

app.use(express.json());

/* ---------------- FILES ---------------- */

const uploadsPath = path.join(__dirname, "uploads");
const avatarsPath = path.join(uploadsPath, "avatars");
const artworksPath = path.join(uploadsPath, "artworks");

if (!fs.existsSync(avatarsPath)) {
  fs.mkdirSync(avatarsPath, { recursive: true });
}

if (!fs.existsSync(artworksPath)) {
  fs.mkdirSync(artworksPath, { recursive: true });
}

app.use("/uploads", express.static(uploadsPath));

const storage = multer.diskStorage({
  destination: (req, file, cb) => cb(null, avatarsPath),
  filename: (req, file, cb) => {
    const uniqueSuffix = Date.now() + "-" + Math.round(Math.random() * 1e9);

    let extension = ".jpg";
    if (file.mimetype === "image/png") extension = ".png";
    else if (file.mimetype === "image/webp") extension = ".webp";
    else if (file.mimetype === "image/jpeg") extension = ".jpg";

    cb(null, uniqueSuffix + extension);
  },
});

const fileFilter = (req, file, cb) => {
  if (file.mimetype.startsWith("image/")) cb(null, true);
  else cb(new Error("Only image files are allowed"), false);
};

const upload = multer({ storage, fileFilter });

const artworkStorage = multer.diskStorage({
  destination: (req, file, cb) => cb(null, artworksPath),
  filename: (req, file, cb) => {
    const uniqueSuffix = Date.now() + "-" + Math.round(Math.random() * 1e9);
    let extension = ".png";
    if (file.mimetype === "image/jpeg") extension = ".jpg";
    else if (file.mimetype === "image/webp") extension = ".webp";
    else if (file.mimetype === "image/png") extension = ".png";
    cb(null, "art-" + uniqueSuffix + extension);
  },
});

const artworkUpload = multer({ storage: artworkStorage, fileFilter });

/* ---------------- HELPERS ---------------- */

function formatUserProfile(user) {
  return {
    id: user.id,
    username: user.username,
    email: user.email,
    avatarUrl: user.avatarUrl,
    bio: user.bio,
    points: user.points,
    title: user.title,
    createdAt: user.createdAt,
    updatedAt: user.updatedAt,
  };
}

function formatProfileMeResponse(user) {
  return {
    id: user.id,
    userId: user.id,
    displayName: user.username || user.email,
    bio: user.bio || "",
    avatarUrl: user.avatarUrl || "",
    progress: {
      id: "local",
      profileId: user.id,
      xp: String(user.points),
    },
  };
}

function tryDeleteAvatarFile(avatarUrl) {
  if (!avatarUrl || !avatarUrl.startsWith("/uploads/avatars/")) {
    return;
  }

  const filePath = path.join(__dirname, avatarUrl.replace(/^\//, ""));
  fs.unlink(filePath, () => {});
}

function tryDeleteArtworkFile(imageUrl) {
  if (!imageUrl || !imageUrl.startsWith("/uploads/artworks/")) {
    return;
  }

  const filePath = path.join(__dirname, imageUrl.replace(/^\//, ""));
  fs.unlink(filePath, () => {});
}

function getTitleByPoints(points) {
  if (points >= 301) return "Мастер галереи";
  if (points >= 201) return "Искусствовед";
  if (points >= 101) return "Знаток";
  return "Новичок";
}

function signToken(userId) {
  return jwt.sign({ sub: userId }, JWT_SECRET, { expiresIn: "30d" });
}

function authMiddleware(req, res, next) {
  const h = req.headers.authorization;
  if (!h || !h.startsWith("Bearer ")) {
    return res.status(401).json({ message: "Unauthorized" });
  }
  try {
    const payload = jwt.verify(h.slice(7), JWT_SECRET);
    req.userId = payload.sub;
    next();
  } catch {
    return res.status(401).json({ message: "Invalid token" });
  }
}

function formatAuthUser(user) {
  return {
    id: user.id,
    name: user.username || user.email.split("@")[0],
    email: user.email,
    emailVerified: true,
    image: user.avatarUrl || null,
  };
}

function publicBase(req) {
  if (process.env.PUBLIC_BASE_URL) {
    return String(process.env.PUBLIC_BASE_URL).replace(/\/$/, "");
  }
  const host = req.get("host") || `localhost:${PORT}`;
  const proto = req.protocol || "http";
  return `${proto}://${host}`;
}

function fullUploadUrl(base, urlPath) {
  if (!urlPath) return "";
  if (urlPath.startsWith("http")) return urlPath;
  return base + urlPath;
}

function formatArtwork(artworkRow, base) {
  const img = fullUploadUrl(base, artworkRow.imageUrl);
  const thumb = artworkRow.thumbnailUrl
    ? fullUploadUrl(base, artworkRow.thumbnailUrl)
    : img;
  return {
    id: artworkRow.id,
    title: artworkRow.title,
    description: artworkRow.description,
    kind: artworkRow.kind,
    source: artworkRow.source,
    status: artworkRow.status,
    schemaVersion: artworkRow.schemaVersion,
    imageUrl: img,
    thumbnailUrl: thumb,
    publishedAt: "",
    createdAt: artworkRow.createdAt.toISOString(),
    updatedAt: artworkRow.updatedAt.toISOString(),
  };
}

const NOTE_CATEGORIES = ["idea", "liked", "question", "important", "todo"];

function normalizeNoteCategory(value) {
  const category = (value || "").trim();
  return NOTE_CATEGORIES.includes(category) ? category : "idea";
}

function formatNote(noteRow) {
  return {
    id: noteRow.id,
    text: noteRow.text,
    category: noteRow.category,
    exhibitId: noteRow.exhibitId || "",
    createdAt: noteRow.createdAt.toISOString(),
    updatedAt: noteRow.updatedAt.toISOString(),
  };
}

/* ---------------- BASE ---------------- */

app.get("/", (req, res) => {
  res.send("Server is working");
});

/* ---------------- AUTH ---------------- */

app.post("/auth/register", async (req, res) => {
  try {
    const { username, email, password, bio } = req.body;

    const existingUser = await prisma.user.findUnique({
      where: { email },
    });

    if (existingUser) {
      return res.status(400).json({ message: "User already exists" });
    }

    const passwordHash = await bcrypt.hash(password, 10);

    const user = await prisma.user.create({
      data: { username, email, passwordHash, bio: bio || null },
    });

    res.json({
      ...formatUserProfile(user),
      token: signToken(user.id),
    });
  } catch (err) {
    res.status(500).json({ message: "Server error" });
  }
});

app.post("/auth/login", async (req, res) => {
  try {
    const { email, password } = req.body;

    const user = await prisma.user.findUnique({
      where: { email },
    });

    if (!user) {
      return res.status(400).json({ message: "User not found" });
    }

    const isPasswordValid = await bcrypt.compare(password, user.passwordHash);

    if (!isPasswordValid) {
      return res.status(400).json({ message: "Invalid password" });
    }

    res.json({
      ...formatUserProfile(user),
      message: "Login successful",
      token: signToken(user.id),
    });
  } catch {
    res.status(500).json({ message: "Server error" });
  }
});

app.post("/auth/sign-in/email", async (req, res) => {
  try {
    const email = req.body?.email?.trim();
    const password = req.body?.password;

    const user = await prisma.user.findUnique({
      where: { email },
    });

    if (!user) {
      return res.status(400).json({ message: "User not found" });
    }

    const isPasswordValid = await bcrypt.compare(password, user.passwordHash);

    if (!isPasswordValid) {
      return res.status(400).json({ message: "Invalid password" });
    }

    res.json({
      redirect: false,
      token: signToken(user.id),
      user: formatAuthUser(user),
    });
  } catch {
    res.status(500).json({ message: "Server error" });
  }
});

app.post("/auth/sign-up/email", async (req, res) => {
  try {
    const name = req.body?.name?.trim();
    const email = req.body?.email?.trim();
    const password = req.body?.password;

    if (!email || !password) {
      return res.status(400).json({ message: "Email and password required" });
    }

    const existingUser = await prisma.user.findUnique({
      where: { email },
    });

    if (existingUser) {
      return res.status(400).json({ message: "User already exists" });
    }

    const passwordHash = await bcrypt.hash(password, 10);

    const user = await prisma.user.create({
      data: {
        username: name || null,
        email,
        passwordHash,
        bio: null,
      },
    });

    res.json({
      redirect: false,
      token: signToken(user.id),
      user: formatAuthUser(user),
    });
  } catch {
    res.status(500).json({ message: "Server error" });
  }
});

app.get("/auth/get-session", authMiddleware, async (req, res) => {
  try {
    const user = await prisma.user.findUnique({
      where: { id: req.userId },
    });

    if (!user) {
      return res.status(401).json({ message: "Unauthorized" });
    }

    const token = req.headers.authorization.slice(7);

    res.json({
      session: {
        id: "current",
        token,
        userId: user.id,
      },
      user: formatAuthUser(user),
    });
  } catch {
    res.status(500).json({ message: "Server error" });
  }
});

app.get("/profile/me", authMiddleware, async (req, res) => {
  try {
    const user = await prisma.user.findUnique({
      where: { id: req.userId },
    });

    if (!user) {
      return res.status(404).json({ message: "User not found" });
    }

    res.json(formatProfileMeResponse(user));
  } catch {
    res.status(500).json({ message: "Server error" });
  }
});

/**
 * Текущий пользователь (JWT): JSON { username, bio } и/или multipart с полем avatar.
 * Должен быть зарегистрирован до /profile/:id, чтобы "me" не воспринимался как id.
 */
app.patch(
  "/profile/me",
  authMiddleware,
  (req, res, next) => {
    const ct = req.headers["content-type"] || "";
    if (ct.includes("multipart/form-data")) {
      return upload.single("avatar")(req, res, (err) => {
        if (err) return res.status(400).json({ message: err.message });
        next();
      });
    }
    next();
  },
  async (req, res) => {
    try {
      const data = {};

      if (req.file) {
        data.avatarUrl = `/uploads/avatars/${req.file.filename}`;
      }

      const body = req.body || {};
      if (body.username !== undefined) {
        const u = String(body.username).trim();
        data.username = u === "" ? null : u;
      }
      if (body.bio !== undefined) {
        const b = body.bio;
        data.bio = b === null || b === "" ? null : String(b);
      }

      if (Object.keys(data).length === 0) {
        return res.status(400).json({ message: "No fields to update" });
      }

      const user = await prisma.user.update({
        where: { id: req.userId },
        data,
      });

      res.json(formatProfileMeResponse(user));
    } catch (e) {
      console.error(e);
      res.status(500).json({ message: "Server error" });
    }
  }
);

app.delete("/profile/me/avatar", authMiddleware, async (req, res) => {
  try {
    const user = await prisma.user.findUnique({
      where: { id: req.userId },
    });

    if (!user) {
      return res.status(404).json({ message: "User not found" });
    }

    if (user.avatarUrl) {
      tryDeleteAvatarFile(user.avatarUrl);
    }

    const updatedUser = await prisma.user.update({
      where: { id: req.userId },
      data: { avatarUrl: null },
    });

    res.json(formatProfileMeResponse(updatedUser));
  } catch (e) {
    console.error(e);
    res.status(500).json({ message: "Server error" });
  }
});

app.post("/auth/sign-out", authMiddleware, (req, res) => {
  res.json({});
});

/* ---------------- PROFILE ---------------- */

app.get("/profile/:id", async (req, res) => {
  try {
    const user = await prisma.user.findUnique({
      where: { id: req.params.id },
    });

    if (!user) return res.status(404).json({ message: "User not found" });

    res.json(formatUserProfile(user));
  } catch {
    res.status(500).json({ message: "Server error" });
  }
});

app.patch("/profile/:id", async (req, res) => {
  try {
    const user = await prisma.user.update({
      where: { id: req.params.id },
      data: req.body,
    });

    res.json(formatUserProfile(user));
  } catch {
    res.status(500).json({ message: "Server error" });
  }
});

app.patch("/profile/:id/avatar", (req, res) => {
  upload.single("avatar")(req, res, async (err) => {
    if (err) return res.status(400).json({ message: err.message });

    const avatarUrl = `/uploads/avatars/${req.file.filename}`;

    const user = await prisma.user.update({
      where: { id: req.params.id },
      data: { avatarUrl },
    });

    res.json(formatUserProfile(user));
  });
});

app.post("/profile/:id/add-points", async (req, res) => {
  try {
    const { points } = req.body;

    const user = await prisma.user.findUnique({
      where: { id: req.params.id },
    });

    if (!user) return res.status(404).json({ message: "User not found" });

    const newPoints = points;
    const newTitle = getTitleByPoints(newPoints);

    const updatedUser = await prisma.user.update({
      where: { id: req.params.id },
      data: { points: newPoints, title: newTitle },
    });

    res.json({
      points: updatedUser.points,
      title: updatedUser.title,
    });
  } catch {
    res.status(500).json({ message: "Server error" });
  }
});

/* ---------------- ARTWORKS (PaintCanvas → профиль, Unity BackendAuthApiClient) ---------------- */

app.get("/artworks/me", authMiddleware, async (req, res) => {
  try {
    const list = await prisma.artwork.findMany({
      where: { userId: req.userId },
      orderBy: { updatedAt: "desc" },
    });
    const base = publicBase(req);
    res.json(list.map((a) => formatArtwork(a, base)));
  } catch {
    res.status(500).json({ message: "Server error" });
  }
});

app.get("/artworks/me/:id", authMiddleware, async (req, res) => {
  try {
    const artwork = await prisma.artwork.findFirst({
      where: { id: req.params.id, userId: req.userId },
    });

    if (!artwork) {
      return res.status(404).json({ message: "Not found" });
    }

    res.json(formatArtwork(artwork, publicBase(req)));
  } catch {
    res.status(500).json({ message: "Server error" });
  }
});

app.post(
  "/artworks/me",
  authMiddleware,
  artworkUpload.fields([
    { name: "image", maxCount: 1 },
    { name: "thumbnail", maxCount: 1 },
  ]),
  async (req, res) => {
    try {
      const imageFile = req.files?.image?.[0];
      if (!imageFile) {
        return res.status(400).json({ message: "image file required" });
      }

      const title = req.body?.title?.trim();
      if (!title) {
        return res.status(400).json({ message: "title required" });
      }

      const thumbFile = req.files?.thumbnail?.[0];
      const imageUrl = `/uploads/artworks/${imageFile.filename}`;
      const thumbnailUrl = thumbFile
        ? `/uploads/artworks/${thumbFile.filename}`
        : imageUrl;

      const schemaVersion = parseInt(req.body?.schemaVersion ?? "1", 10) || 1;

      const artwork = await prisma.artwork.create({
        data: {
          userId: req.userId,
          title,
          description: req.body?.description?.trim() || null,
          kind: req.body?.kind?.trim() || "painting",
          source: req.body?.source?.trim() || "paint-canvas",
          status: req.body?.status?.trim() || "DRAFT",
          schemaVersion,
          imageUrl,
          thumbnailUrl,
        },
      });

      res.json(formatArtwork(artwork, publicBase(req)));
    } catch (e) {
      console.error(e);
      res.status(500).json({ message: "Server error" });
    }
  }
);

app.patch(
  "/artworks/me/:id",
  authMiddleware,
  artworkUpload.fields([
    { name: "image", maxCount: 1 },
    { name: "thumbnail", maxCount: 1 },
  ]),
  async (req, res) => {
    try {
      const existing = await prisma.artwork.findFirst({
        where: { id: req.params.id, userId: req.userId },
      });

      if (!existing) {
        return res.status(404).json({ message: "Not found" });
      }

      const data = {};
      if (req.body?.title !== undefined) {
        data.title = req.body.title?.trim() || null;
      }
      if (req.body?.description !== undefined) {
        data.description = req.body.description?.trim() || null;
      }
      if (req.body?.kind !== undefined) data.kind = req.body.kind?.trim() || existing.kind;
      if (req.body?.source !== undefined) {
        data.source = req.body.source?.trim() || existing.source;
      }
      if (req.body?.status !== undefined) {
        data.status = req.body.status?.trim() || existing.status;
      }
      if (req.body?.schemaVersion !== undefined) {
        data.schemaVersion =
          parseInt(req.body.schemaVersion, 10) || existing.schemaVersion;
      }

      const imageFile = req.files?.image?.[0];
      const thumbFile = req.files?.thumbnail?.[0];

      if (imageFile) {
        data.imageUrl = `/uploads/artworks/${imageFile.filename}`;
        data.thumbnailUrl = thumbFile
          ? `/uploads/artworks/${thumbFile.filename}`
          : data.imageUrl;
      } else if (thumbFile) {
        data.thumbnailUrl = `/uploads/artworks/${thumbFile.filename}`;
      }

      const artwork = await prisma.artwork.update({
        where: { id: existing.id },
        data,
      });

      res.json(formatArtwork(artwork, publicBase(req)));
    } catch (e) {
      console.error(e);
      res.status(500).json({ message: "Server error" });
    }
  }
);

app.delete("/artworks/me/:id", authMiddleware, async (req, res) => {
  try {
    const existing = await prisma.artwork.findFirst({
      where: { id: req.params.id, userId: req.userId },
    });

    if (!existing) {
      return res.status(404).json({ message: "Not found" });
    }

    await prisma.artwork.delete({ where: { id: existing.id } });

    tryDeleteArtworkFile(existing.imageUrl);
    if (existing.thumbnailUrl && existing.thumbnailUrl !== existing.imageUrl) {
      tryDeleteArtworkFile(existing.thumbnailUrl);
    }

    res.json({ id: existing.id });
  } catch (e) {
    console.error(e);
    res.status(500).json({ message: "Server error" });
  }
});

/* ---------------- NOTES (заметки пользователя: цвет = категория) ---------------- */

app.get("/notes/me", authMiddleware, async (req, res) => {
  try {
    const list = await prisma.note.findMany({
      where: { userId: req.userId },
      orderBy: { updatedAt: "desc" },
    });
    res.json(list.map(formatNote));
  } catch (e) {
    console.error(e);
    res.status(500).json({ message: "Server error" });
  }
});

app.post("/notes/me", authMiddleware, async (req, res) => {
  try {
    const text = (req.body?.text || "").trim();
    if (!text) {
      return res.status(400).json({ message: "text required" });
    }

    const note = await prisma.note.create({
      data: {
        userId: req.userId,
        text,
        category: normalizeNoteCategory(req.body?.category),
        exhibitId: (req.body?.exhibitId || "").trim() || null,
      },
    });

    res.json(formatNote(note));
  } catch (e) {
    console.error(e);
    res.status(500).json({ message: "Server error" });
  }
});

app.patch("/notes/me/:id", authMiddleware, async (req, res) => {
  try {
    const existing = await prisma.note.findFirst({
      where: { id: req.params.id, userId: req.userId },
    });

    if (!existing) {
      return res.status(404).json({ message: "Not found" });
    }

    const data = {};
    if (req.body?.text !== undefined) {
      const text = (req.body.text || "").trim();
      if (!text) {
        return res.status(400).json({ message: "text required" });
      }
      data.text = text;
    }
    if (req.body?.category !== undefined) {
      data.category = normalizeNoteCategory(req.body.category);
    }
    if (req.body?.exhibitId !== undefined) {
      data.exhibitId = (req.body.exhibitId || "").trim() || null;
    }

    const note = await prisma.note.update({
      where: { id: existing.id },
      data,
    });

    res.json(formatNote(note));
  } catch (e) {
    console.error(e);
    res.status(500).json({ message: "Server error" });
  }
});

app.delete("/notes/me/:id", authMiddleware, async (req, res) => {
  try {
    const existing = await prisma.note.findFirst({
      where: { id: req.params.id, userId: req.userId },
    });

    if (!existing) {
      return res.status(404).json({ message: "Not found" });
    }

    await prisma.note.delete({ where: { id: existing.id } });
    res.json({ id: existing.id });
  } catch (e) {
    console.error(e);
    res.status(500).json({ message: "Server error" });
  }
});

/* ---------------- EXHIBIT FAVORITES ---------------- */

app.get("/favorites/exhibits", authMiddleware, async (req, res) => {
  try {
    const favorites = await prisma.exhibitFavorite.findMany({
      where: { userId: req.userId },
      orderBy: { createdAt: "desc" },
      select: { exhibitId: true },
    });
    res.json(favorites.map((item) => item.exhibitId));
  } catch (e) {
    console.error(e);
    res.status(500).json({ message: "Server error" });
  }
});

app.post("/favorites/exhibits/:exhibitId", authMiddleware, async (req, res) => {
  try {
    const exhibitId = String(req.params.exhibitId || "").trim();
    if (!exhibitId) {
      return res.status(400).json({ message: "exhibitId required" });
    }

    const favorite = await prisma.exhibitFavorite.upsert({
      where: {
        userId_exhibitId: {
          userId: req.userId,
          exhibitId,
        },
      },
      update: {},
      create: {
        userId: req.userId,
        exhibitId,
      },
    });

    res.json({ exhibitId: favorite.exhibitId, createdAt: favorite.createdAt.toISOString() });
  } catch (e) {
    console.error(e);
    res.status(500).json({ message: "Server error" });
  }
});

app.delete("/favorites/exhibits/:exhibitId", authMiddleware, async (req, res) => {
  try {
    const exhibitId = String(req.params.exhibitId || "").trim();
    if (!exhibitId) {
      return res.status(400).json({ message: "exhibitId required" });
    }

    await prisma.exhibitFavorite.deleteMany({
      where: {
        userId: req.userId,
        exhibitId,
      },
    });

    res.json({ ok: true });
  } catch (e) {
    console.error(e);
    res.status(500).json({ message: "Server error" });
  }
});

/* ---------------- EXHIBIT PROGRESS ---------------- */

const EXHIBIT_VIEW_XP = 10;

app.get("/progress/exhibits/views", authMiddleware, async (req, res) => {
  try {
    const views = await prisma.exhibitView.findMany({
      where: { userId: req.userId },
      orderBy: { createdAt: "desc" },
      select: { exhibitId: true },
    });
    res.json(views.map((item) => item.exhibitId));
  } catch (e) {
    console.error(e);
    res.status(500).json({ message: "Server error" });
  }
});

app.post("/progress/exhibits/:exhibitId/view", authMiddleware, async (req, res) => {
  try {
    const exhibitId = String(req.params.exhibitId || "").trim();
    if (!exhibitId) {
      return res.status(400).json({ message: "exhibitId required" });
    }

    const user = await prisma.user.findUnique({
      where: { id: req.userId },
    });

    if (!user) {
      return res.status(404).json({ message: "User not found" });
    }

    const existingView = await prisma.exhibitView.findUnique({
      where: {
        userId_exhibitId: {
          userId: req.userId,
          exhibitId,
        },
      },
    });

    if (existingView) {
      return res.json({
        exhibitId,
        applied: false,
        awardedXp: 0,
        previousXp: user.points,
        totalXp: user.points,
      });
    }

    const previousXp = user.points;
    const totalXp = previousXp + EXHIBIT_VIEW_XP;
    const title = getTitleByPoints(totalXp);

    await prisma.$transaction([
      prisma.exhibitView.create({
        data: {
          userId: req.userId,
          exhibitId,
        },
      }),
      prisma.user.update({
        where: { id: req.userId },
        data: {
          points: totalXp,
          title,
        },
      }),
    ]);

    res.json({
      exhibitId,
      applied: true,
      awardedXp: EXHIBIT_VIEW_XP,
      previousXp,
      totalXp,
    });
  } catch (e) {
    console.error(e);
    res.status(500).json({ message: "Server error" });
  }
});

/* ---------------- GUIDE (Мартишка, без ИИ) ---------------- */

const guideDataPath = path.join(__dirname, "data", "guide.json");
let guideCache = null;

function loadGuideData() {
  guideCache = JSON.parse(fs.readFileSync(guideDataPath, "utf8"));
  return guideCache;
}

function normalizeGuideText(s) {
  return String(s || "")
    .toLowerCase()
    .replace(/[''`]/g, "'")
    .replace(/[^a-zа-яё0-9\s']/gi, " ")
    .replace(/\s+/g, " ")
    .trim();
}

const GUIDE_OFF_TOPIC_WORDS = [
  "погод",
  "рецепт",
  "курс доллар",
  "футбол",
  "политик",
  "войн",
  "крипт",
  "биткоин",
  "знаком",
  "свидан",
  "диет",
  "лечен",
  "врач",
  "программирован",
  "unity",
  "android",
  "iphone",
];

function isGuideOffTopic(messageNorm) {
  return GUIDE_OFF_TOPIC_WORDS.some((w) => messageNorm.includes(w));
}

function guideMessageVariants(messageNorm) {
  const variants = [messageNorm];
  const stripped = messageNorm
    .replace(/^(расскажи|подскажи|объясни|скажи|расскажите|подскажите)\s+(мне\s+)?(про\s+)?/u, "")
    .replace(/^(что\s+такое|что\s+значит|что\s+это|кто\s+такой|кто\s+такая|какой\s+это|какая\s+это)\s+/u, "")
    .replace(/^(как|где|когда|почему|зачем|сколько)\s+/u, "")
    .trim();

  if (stripped && stripped !== messageNorm) variants.push(stripped);
  return variants;
}

function findGuideTopic(guide, messageNorm) {
  const variants = guideMessageVariants(messageNorm);
  let best = null;
  let bestScore = 0;
  let bestMaxKw = 0;

  for (const topic of guide.topics) {
    if (!topic.keywords?.length) continue;

    let score = 0;
    let maxKw = 0;
    const matched = new Set();

    for (const variant of variants) {
      for (const kw of topic.keywords) {
        const k = normalizeGuideText(kw);
        if (k && variant.includes(k) && !matched.has(k)) {
          matched.add(k);
          const pts = Math.max(k.length, 3);
          score += pts;
          maxKw = Math.max(maxKw, k.length);
        }
      }
    }

    if (
      score > bestScore ||
      (score === bestScore && score > 0 && maxKw > bestMaxKw)
    ) {
      bestScore = score;
      bestMaxKw = maxKw;
      best = topic;
    }
  }

  return bestScore > 0 ? best : null;
}

/** Подсказки-чипы для чата (первый заход и т.д.) */
app.get("/guide/suggestions", (req, res) => {
  try {
    const guide = loadGuideData();
    res.json({
      welcome: guide.welcome,
      suggestions: guide.suggestions,
    });
  } catch (e) {
    console.error(e);
    res.status(500).json({ message: "Guide data error" });
  }
});

/**
 * Чат без ИИ.
 * Body: { message?: string, topicId?: string }
 * — topicId: нажали чип (museum, artists, …)
 * — message: свой текст
 */
app.post("/guide/chat", (req, res) => {
  try {
    const guide = loadGuideData();
    const topicId = req.body?.topicId?.trim();
    const message = req.body?.message?.trim();
    const messageNorm = normalizeGuideText(message);

    if (topicId) {
      const topic = guide.topics.find((t) => t.id === topicId);
      if (!topic) {
        return res.status(400).json({ message: "Unknown topicId" });
      }
      return res.json({
        reply: topic.answer,
        topicId: topic.id,
        source: "chip",
      });
    }

    if (!message) {
      return res.status(400).json({ message: "message or topicId required" });
    }

    if (isGuideOffTopic(messageNorm)) {
      return res.json({
        reply: guide.offTopic,
        topicId: null,
        source: "off_topic",
      });
    }

    const topic = findGuideTopic(guide, messageNorm);
    if (topic) {
      return res.json({
        reply: topic.answer,
        topicId: topic.id,
        source: "keyword",
      });
    }

    res.json({
      reply: guide.noMatch,
      topicId: null,
      source: "no_match",
    });
  } catch (e) {
    console.error(e);
    res.status(500).json({ message: "Server error" });
  }
});

/* ---------------- QUIZ ---------------- */

function shuffleArray(items) {
  const arr = [...items];
  for (let i = arr.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [arr[i], arr[j]] = [arr[j], arr[i]];
  }
  return arr;
}

app.get("/quiz/questions", async (req, res) => {
  try {
    const rawCount = parseInt(req.query.count, 10);
    const count =
      Number.isFinite(rawCount) && rawCount > 0 ? rawCount : null;

    const questions = await prisma.quizQuestion.findMany({
      include: { answers: { orderBy: { sortOrder: "asc" } } },
    });

    const shuffled = shuffleArray(questions);
    const result = count
      ? shuffled.slice(0, Math.min(count, shuffled.length))
      : shuffled;

    res.json(result);
  } catch (e) {
    console.error(e);
    res.status(500).json({ message: "Server error" });
  }
});

/* 🔥 ГЛАВНЫЙ ФИКС ДУБЛЕЙ */

app.post("/quiz/add", async (req, res) => {
  console.log("🔥 QUIZ ADD REQUEST");
  console.log("BODY:", req.body);
  console.log("TIME:", new Date().toISOString());

  try {
    let { question, fact, correctIndex, answers } = req.body;

    // 🔥 нормализация (очень важно)
    question = question?.trim();

    if (!question) {
      return res.status(400).json({ message: "Empty question" });
    }

    // 🔥 защита от дублей
    const existing = await prisma.quizQuestion.findFirst({
      where: {
        question: {
          equals: question,
        },
      },
    });

    if (existing) {
      console.log("⚠️ Duplicate blocked:", question);
      return res.json({ message: "Already exists" });
    }

    const newQuestion = await prisma.quizQuestion.create({
      data: {
        question,
        fact,
        correctIndex,
        answers: {
          create: answers.map((text, index) => ({
            text: text.trim(),
            sortOrder: index,
          })),
        },
      },
      include: { answers: true },
    });

    res.json(newQuestion);
  } catch (err) {
    console.log(err);
    res.status(500).json({ message: "Server error" });
  }
});

/* ---------------- START ---------------- */

app.listen(PORT, "0.0.0.0", () => {
  console.log(`Server started on http://localhost:${PORT}`);
});
