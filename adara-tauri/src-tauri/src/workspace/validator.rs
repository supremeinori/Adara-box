use std::path::Path;

use super::models::ResourceType;

const MAX_WORKSPACE_NAME_LEN: usize = 60;
const MAX_RESOURCE_NAME_LEN: usize = 100;
const MAX_TARGET_LEN: usize = 2048;
const MAX_ICON_LEN: usize = 512;

fn has_control_chars(s: &str) -> bool {
    s.chars().any(|c| c < '\u{20}' || c == '\u{7f}')
}

pub fn validate_workspace_name(raw: &str) -> Result<String, String> {
    let trimmed = raw.trim();
    if trimmed.is_empty() {
        return Err("Workspace name cannot be empty.".into());
    }
    if has_control_chars(trimmed) {
        return Err("Workspace name contains invalid control characters.".into());
    }
    if trimmed.chars().count() > MAX_WORKSPACE_NAME_LEN {
        return Err(format!(
            "Workspace name exceeds {MAX_WORKSPACE_NAME_LEN} characters."
        ));
    }
    Ok(trimmed.to_string())
}

pub fn validate_workspace_icon(raw: Option<&str>) -> Result<String, String> {
    let icon = raw.unwrap_or("📁").trim();
    if icon.is_empty() {
        return Ok("📁".into());
    }
    if has_control_chars(icon) {
        return Err("Workspace icon contains invalid control characters.".into());
    }
    if let Some(rel) = icon.strip_prefix("custom:") {
        let safe = rel.trim();
        if safe.is_empty()
            || safe.contains('/')
            || safe.contains('\\')
            || safe.contains("..")
            || safe.len() > 100
        {
            return Err("Invalid custom workspace icon filename.".into());
        }
        return Ok(format!("custom:{safe}"));
    }
    if icon.chars().count() > MAX_ICON_LEN {
        return Err("Workspace icon value is too long.".into());
    }
    Ok(icon.to_string())
}

pub fn validate_resource_name(raw: &str) -> Result<String, String> {
    let trimmed = raw.trim();
    if trimmed.is_empty() {
        return Err("Resource name cannot be empty.".into());
    }
    if has_control_chars(trimmed) {
        return Err("Resource name contains invalid control characters.".into());
    }
    if trimmed.chars().count() > MAX_RESOURCE_NAME_LEN {
        return Err(format!(
            "Resource name exceeds {MAX_RESOURCE_NAME_LEN} characters."
        ));
    }
    Ok(trimmed.to_string())
}

pub fn validate_and_normalize_target(
    resource_type: ResourceType,
    raw: &str,
) -> Result<String, String> {
    let trimmed = raw.trim().trim_matches('"').trim();
    if trimmed.is_empty() {
        return Err("Target path or URL cannot be empty.".into());
    }
    if has_control_chars(trimmed) || trimmed.contains('\0') {
        return Err("Target contains invalid control characters.".into());
    }
    if trimmed.chars().count() > MAX_TARGET_LEN {
        return Err(format!("Target exceeds {MAX_TARGET_LEN} characters."));
    }

    match resource_type {
        ResourceType::Link => normalize_link(trimmed),
        ResourceType::Application | ResourceType::File | ResourceType::Folder => {
            if trimmed.chars().count() > 1024 {
                return Err("File or folder path exceeds 1024 characters.".into());
            }
            Ok(trimmed.to_string())
        }
    }
}

pub fn normalize_link(raw: &str) -> Result<String, String> {
    let trimmed = raw.trim();
    let lower = trimmed.to_ascii_lowercase();

    for blocked in ["javascript:", "vbscript:", "data:", "file:"] {
        if lower.starts_with(blocked) {
            return Err(format!("Unsupported URL scheme: {blocked}"));
        }
    }

    let candidate = if lower.starts_with("http://")
        || lower.starts_with("https://")
        || lower.starts_with("mailto:")
    {
        trimmed.to_string()
    } else if lower.contains("://") {
        return Err("Only http://, https://, and mailto: links are allowed.".into());
    } else if !trimmed.starts_with('\\') && !trimmed.contains('\\') {
        format!("https://{trimmed}")
    } else {
        return Err("Enter a valid web URL starting with https:// or http://.".into());
    };

    if candidate.contains(char::is_whitespace) {
        return Err("URL cannot contain spaces.".into());
    }
    let after_scheme = candidate
        .split_once("://")
        .map(|(_, rest)| rest)
        .or_else(|| candidate.strip_prefix("mailto:"))
        .unwrap_or("");
    if after_scheme.is_empty() {
        return Err("URL host cannot be empty.".into());
    }
    Ok(candidate)
}

/// Infers `(ResourceType, clean_target, display_name)` from a raw dropped path or URL.
pub fn infer_resource_from_raw(raw: &str) -> Result<(ResourceType, String, String), String> {
    let trimmed = raw.trim().trim_matches('"').trim();
    if trimmed.is_empty() {
        return Err("Empty resource input.".into());
    }
    let lower = trimmed.to_ascii_lowercase();

    if lower.starts_with("http://")
        || lower.starts_with("https://")
        || lower.starts_with("mailto:")
    {
        let normalized = normalize_link(trimmed)?;
        let fallback_name = friendly_link_name(&normalized);
        return Ok((ResourceType::Link, normalized, fallback_name));
    }

    if lower.ends_with(".url") {
        if let Some((url, title)) = parse_internet_shortcut(Path::new(trimmed)) {
            if let Ok(normalized) = normalize_link(&url) {
                let name = if title.is_empty() {
                    friendly_link_name(&normalized)
                } else {
                    title
                };
                return Ok((ResourceType::Link, normalized, name));
            }
        }
    }

    let path = Path::new(trimmed);
    let stem = path
        .file_stem()
        .and_then(|s| s.to_str())
        .map(str::trim)
        .filter(|s| !s.is_empty());
    let file_name = path
        .file_name()
        .and_then(|s| s.to_str())
        .map(str::trim)
        .filter(|s| !s.is_empty());

    if path.is_dir() {
        let name = file_name.unwrap_or(trimmed).to_string();
        return Ok((ResourceType::Folder, trimmed.to_string(), name));
    }

    if lower.ends_with(".lnk") {
        if let Some((resolved, _)) = crate::utils::resolve_shortcut(trimmed) {
            let rp = Path::new(&resolved);
            let name = stem.or(file_name).unwrap_or(trimmed).to_string();
            if rp.is_dir() {
                return Ok((ResourceType::Folder, trimmed.to_string(), name));
            }
            let r_lower = resolved.to_ascii_lowercase();
            if is_app_extension(&r_lower) {
                return Ok((ResourceType::Application, trimmed.to_string(), name));
            }
            return Ok((ResourceType::File, trimmed.to_string(), name));
        }
    }

    if is_app_extension(&lower) {
        let name = stem.or(file_name).unwrap_or(trimmed).to_string();
        return Ok((ResourceType::Application, trimmed.to_string(), name));
    }

    if path.is_file() || trimmed.contains('\\') || trimmed.contains('/') {
        let name = file_name.or(stem).unwrap_or(trimmed).to_string();
        return Ok((ResourceType::File, trimmed.to_string(), name));
    }

    // Bare executable name on PATH / App Paths or bare web domain.
    if crate::utils::resolve_executable_path(trimmed).is_some() {
        let name = stem.unwrap_or(trimmed).to_string();
        return Ok((ResourceType::Application, trimmed.to_string(), name));
    }

    if trimmed.contains('.') && !trimmed.contains(' ') {
        let normalized = normalize_link(trimmed)?;
        let name = friendly_link_name(&normalized);
        return Ok((ResourceType::Link, normalized, name));
    }

    Err(format!("Could not recognize resource target: {trimmed}"))
}

pub fn friendly_link_name(url: &str) -> String {
    let stripped = url
        .split_once("://")
        .map(|(_, rest)| rest)
        .or_else(|| url.strip_prefix("mailto:"))
        .unwrap_or(url);
    let host = stripped.split(['/', '?', '#']).next().unwrap_or(stripped);
    let clean_host = host.strip_prefix("www.").unwrap_or(host).trim();
    if clean_host.is_empty() {
        "Link".into()
    } else {
        clean_host.chars().take(MAX_RESOURCE_NAME_LEN).collect()
    }
}

fn is_app_extension(lower: &str) -> bool {
    [".exe", ".msi", ".bat", ".cmd", ".com", ".msc", ".appref-ms", ".lnk"]
        .iter()
        .any(|ext| lower.ends_with(ext))
}

fn parse_internet_shortcut(path: &Path) -> Option<(String, String)> {
    let content = std::fs::read_to_string(path).ok()?;
    let url = content.lines().find_map(|line| {
        let trimmed = line.trim();
        trimmed
            .strip_prefix("URL=")
            .or_else(|| trimmed.strip_prefix("url="))
            .map(str::trim)
            .filter(|s| !s.is_empty())
            .map(ToString::to_string)
    })?;
    let title = path
        .file_stem()
        .and_then(|s| s.to_str())
        .unwrap_or("")
        .trim()
        .to_string();
    Some((url, title))
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn rejects_blank_and_oversized_workspace_names() {
        assert!(validate_workspace_name("   ").is_err());
        assert!(validate_workspace_name("bad\nname").is_err());
        let long = "a".repeat(61);
        assert!(validate_workspace_name(&long).is_err());
        assert_eq!(validate_workspace_name("  Work  ").unwrap(), "Work");
    }

    #[test]
    fn blocks_unsafe_link_schemes_and_normalizes_domains() {
        assert!(normalize_link("javascript:alert(1)").is_err());
        assert!(normalize_link("file:///C:/Windows/win.ini").is_err());
        assert_eq!(normalize_link("github.com").unwrap(), "https://github.com");
        assert_eq!(
            normalize_link("https://docs.rs/rusqlite").unwrap(),
            "https://docs.rs/rusqlite"
        );
    }

    #[test]
    fn validates_custom_icon_paths_against_traversal() {
        assert!(validate_workspace_icon(Some("custom:../secret.png")).is_err());
        assert!(validate_workspace_icon(Some("custom:ws-123.png")).is_ok());
        assert_eq!(validate_workspace_icon(Some("")).unwrap(), "📁");
        let legacy = r"C:\Users\HYPE AMD\AppData\Local\AdaraBox\workspace-icons\ws_123.jpeg";
        assert_eq!(validate_workspace_icon(Some(legacy)).unwrap(), legacy);
    }
}
