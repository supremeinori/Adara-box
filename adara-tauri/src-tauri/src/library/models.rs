use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "lowercase")]
pub enum BookFileType {
    Pdf,
    Epub,
}

impl BookFileType {
    pub fn as_str(self) -> &'static str {
        match self {
            BookFileType::Pdf => "pdf",
            BookFileType::Epub => "epub",
        }
    }

    pub fn from_ext(ext: &str) -> Result<Self, String> {
        match ext.trim().trim_start_matches('.').to_ascii_lowercase().as_str() {
            "pdf" => Ok(BookFileType::Pdf),
            "epub" => Ok(BookFileType::Epub),
            other => Err(format!("Unsupported book file type: '{other}'. Only PDF and EPUB are supported.")),
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct LibraryBookDto {
    pub id: String,
    pub title: String,
    pub author: String,
    pub category: String,
    pub status: String,
    pub notes: String,
    pub file_type: BookFileType,
    pub file_name: String,
    pub file_size: i64,
    pub has_cover: bool,
    pub current_page: i64,
    pub total_pages: i64,
    pub current_cfi: Option<String>,
    pub reading_progress: f64,
    pub last_read_at: Option<String>,
    pub created_at: String,
    pub updated_at: String,
}

#[derive(Debug, Clone, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct UpdateBookInput {
    pub id: String,
    pub title: String,
    pub author: String,
    pub category: String,
    pub status: String,
    pub notes: String,
    pub cover_data_url: Option<String>,
    pub clear_cover: Option<bool>,
}

#[derive(Debug, Clone, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct BookProgressInput {
    pub id: String,
    pub current_page: Option<i64>,
    pub total_pages: Option<i64>,
    pub current_cfi: Option<String>,
    pub reading_progress: Option<f64>,
    pub status: Option<String>,
}
